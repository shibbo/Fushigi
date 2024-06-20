using Fushigi.course;
using Fushigi.gl;
using Fushigi.gl.Bfres;
using Fushigi.param;
using Fushigi.ui.modal;
using Fushigi.ui.SceneObjects;
using Fushigi.ui.SceneObjects.bgunit;
using Fushigi.ui.undo;
using Fushigi.util;
using ImGuiNET;
using Silk.NET.OpenGL;
using System.Collections.Immutable;
using System.Drawing;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fushigi.rstb;
using Fushigi.course.distance_view;
using Fushigi.ui.helpers;
using Fasterflect;
using System.Text.RegularExpressions;
using System.Collections;

namespace Fushigi.ui.widgets
{
    class CourseScene
    {
        Dictionary<CourseArea, LevelViewport> viewports = [];
        Dictionary<CourseArea, object?> lastSavedAction = [];
        Dictionary<CourseArea, CourseAreaScene> areaScenes = [];
        Dictionary<CourseArea, LevelViewport>? lastCreatedViewports;
        public LevelViewport activeViewport;
        UndoWindow undoWindow;
        Vector3 camSave;

        (object? courseObj, FullPropertyCapture capture)
           propertyCapture = (null,
            FullPropertyCapture.Empty);

        readonly Course course;
        readonly IPopupModalHost mPopupModalHost;
        CourseArea selectedArea;

        readonly Dictionary<string, bool> mLayersVisibility = [];
        bool mHasFilledLayers = false;
        bool mAllLayersVisible = true;
        readonly List<IToolWindow> mOpenToolWindows = [];

        string mActorSearchText = "";

        //CourseLink? mSelectedGlobalLink = null;

        string[] viewMode = [
            "View All Actors", 
            "View Normal Actors", 
            "View Wonder Actors"];

        string[] linkTypes = [
            "BasicSignal",
            "Create",
            "CreateRelativePos",
            "CreateAfterDied",
            "Delete",
            "Reference",
            "NextGoTo",
            "NextGoToParallel",
            "Bind",
            "Bind_NoRot",
            "Connection",
            "Follow",
            "PopUp",
            "Contents",
            "NoticeDeath",
            "Relocation",
            "ParamRefForChild",
            "CullingReference",
            "EventJoinMember",
            "EventGuest_04",
            "EventGuest_05",
            "EventGuest_06",
            "EventGuest_08",
            "EventGuest_09",
            "EventGuest_10",
            "EventGuest_11",
        ];

        public static string[] layerTypes = [
            "DvScreen",
            "DvNear2",
            "DvNear1",
            "DecoAreaFront",
            "PlayArea", 
            "DecoArea",
            "DvMiddle1",
            "DvMiddle2",
            "DvFar1",
            "DvFar2",
            "DvFar3",
            "DvFar4",
            "DvFar5",
            "DvFar6",
            "DvFar7",
            "DvFar8",
            "DvFar9",
            "DvFar10"
        ];
        public static Regex NumberRegex = new(@"\d+");

        // This code sorts the layer order on the layer panel.
        // You can look through it before deciding if it's optimized enough to include.
        // Just uncomment all of this if it is.
        // public static List<string> layerSortTypes = [
        //     "DvScreen",
        //     "DvNear",
        //     "DecoAreaFront",
        //     "PlayArea", 
        //     "DvPlayArea",
        //     "DecoArea",
        //     "DvMiddle",
        //     "DvFar"
        // ];

        // public class LayerSorter : IComparer<string>
        // {
        //     public int Compare(string x, string y)
        //     {
        //         var idX = layerSortTypes.IndexOf(NumberRegex.Replace(x, ""));
        //         var idY = layerSortTypes.IndexOf(NumberRegex.Replace(y, ""));
        //         if(idX != -1)
        //         {
        //                 int result = idY == -1 ? 1:idX.CompareTo(idY);
        //                 if (result != 0)
        //                 {
        //                     return result;
        //                 }
        //                 else
        //                 {
        //                     result = x.Length.CompareTo(x.Length);
        //                     return result != 0 ? result:x.CompareTo(y);
        //                 }
        //         }
        //         else
        //         {
        //                 return idY != -1 ? -1:0;
        //         }
        //     }
        // }
        // readonly LayerSorter layerSort = new();

        public static async Task<CourseScene> Create(Course course, 
            GLTaskScheduler glScheduler, 
            IPopupModalHost popupModalHost,
            IProgress<(string operationName, float? progress)> progress)
        {
            var cs = new CourseScene(course, glScheduler, popupModalHost);

            foreach (var area in course.GetAreas())
            {
                var areaScene = new CourseAreaScene(area, new CourseAreaSceneRoot(area));
                cs.areaScenes[area] = areaScene;
                var viewport = await glScheduler.Schedule(gl => new LevelViewport(area, gl, areaScene));
                cs.viewports[area] = viewport;
                cs.lastSavedAction[area] = null;

                //might not be the best approach but better than what we had before
                viewport.ObjectDeletionRequested += (objs) =>
                {
                    if (objs.Count > 0)
                        _ = cs.DeleteObjectsWithWarningPrompt(objs,
                            areaScene.EditContext, "Delete objects");
                };
            }

            cs.activeViewport = cs.viewports[cs.selectedArea];

            await cs.PrepareResourcesLoad(glScheduler, progress);

            return cs;
        }

        private CourseScene(Course course, GLTaskScheduler glScheduler, IPopupModalHost popupModalHost)
        {
            this.course = course;
            this.mPopupModalHost = popupModalHost;
            selectedArea = course.GetArea(0);
            undoWindow = new UndoWindow();
            activeViewport = null!;
        }

        public async Task PrepareResourcesLoad(GLTaskScheduler glScheduler,
            IProgress<(string operationName, float? progress)> progress)
        {
            //Check what files are needed to load/unload by area
            List<string> resourceFiles = new List<string>();
            foreach (var area in course.GetAreas())
            {
                foreach (var actor in area.GetActors())
                {
                    if (actor.mActorPack != null)
                        resourceFiles.Add(actor.mActorPack.GetModelFileName());
                    
                }
            }
            //All resource files to load
            resourceFiles = resourceFiles.Distinct().Where(x => !string.IsNullOrEmpty(x)).ToList();
            //Unload any unused resources in the cache

            List<string> removed = new List<string>();
            foreach (var bfres in BfresCache.Cache)
            {
                //Not currently used by area, dispose
                if (!resourceFiles.Contains(bfres.Key))
                {
                    bfres.Value.Dispose();
                    removed.Add(bfres.Key);

                    Console.WriteLine($"Disposing resource {bfres.Key}");
                }
            }

            foreach (var bfres in removed)
                BfresCache.Cache.Remove(bfres);

            //Load all used resources
            for (int i = 0; i < resourceFiles.Count; i++)
            {
                string? file = resourceFiles[i];
                progress.Report(($"Loading models", i/(float)resourceFiles.Count));
                await BfresCache.LoadAsync(glScheduler, file);
            }
        }

        public void PreventFurtherRendering()
        {
            foreach (var v in viewports.Values) v.PreventFurtherRendering();
        }

        public void Undo() => areaScenes[selectedArea].EditContext.Undo();
        public void Redo() => areaScenes[selectedArea].EditContext.Redo();

        public bool HasUnsavedChanges()
        {
            foreach (var area in course.GetAreas())
            {
                if (lastSavedAction[area] != areaScenes[area].EditContext.GetLastAction())
                    return true;
            }

            return false;
        }

        public void DrawUI(GL gl, double deltaSeconds)
        {
            UndoHistoryPanel();

            ActorsPanel();

            SelectionParameterPanel();

            RailsPanel();

            GlobalLinksPanel();

            RailLinksPanel();

            LocalLinksPanel();

            BGUnitPanel();

            CourseMiniView();
          
            for (int i = 0; i < mOpenToolWindows.Count; i++)
            {
                var window = mOpenToolWindows[i];
                bool windowOpen = true;
                window.Draw(ref windowOpen);

                if (!windowOpen)
                {
                    mOpenToolWindows.RemoveAt(i);
                    i--;
                }
            }

            ulong selectionVersionBefore = areaScenes[selectedArea].EditContext.SelectionVersion;

            bool status = ImGui.Begin("Viewports", ImGuiWindowFlags.NoNav);

            ImGui.DockSpace(0x100, ImGui.GetContentRegionAvail());

            for (int i = 0; i < course.GetAreaCount(); i++)
            {
                var area = course.GetArea(i);
                var viewport = viewports[area];

                ImGui.SetNextWindowDockID(0x100, ImGuiCond.Once);

                if (ImGui.Begin(area.GetName(), ImGuiWindowFlags.NoNav))
                {
                    if (ImGui.BeginChild("viewport_menu_bar", new Vector2(ImGui.GetWindowWidth(), 30)))
                    {
                        Vector2 icon_size = new Vector2(25, 25);

                        ImGui.PushStyleColor(ImGuiCol.Button, 0);

                        if (ImGui.Button(viewport.PlayAnimations ? IconUtil.ICON_STOP : IconUtil.ICON_PLAY, icon_size))
                            viewport.PlayAnimations = !viewport.PlayAnimations;

                        ImGui.SameLine();

                        if (ImguiHelper.DrawTextToggle(IconUtil.ICON_BORDER_ALL, viewport.ShowGrid, icon_size))
                            viewport.ShowGrid = !viewport.ShowGrid;

                        ImGui.SameLine();

                        string current_palette = area.mInitEnvPalette == null ? "" : area.mInitEnvPalette.Name;

                        void SelectPalette(string name, string palette)
                        {
                            if (string.IsNullOrEmpty(palette))
                                return;

                            palette = palette.Replace("Work/Gyml/Gfx/EnvPaletteParam/", "");
                            palette = palette.Replace(".game__gfx__EnvPaletteParam.gyml", "");

                            bool selected = current_palette == name;
                            if (ImGui.Selectable($"{name} : {palette}", selected))
                                viewport.EnvironmentData.TransitionEnvPalette(current_palette, palette);

                            if (selected)
                                ImGui.SetItemDefaultFocus();
                        }

                        var flags = ImGuiComboFlags.NoArrowButton | ImGuiComboFlags.WidthFitPreview;
                        if (ImGui.BeginCombo($"##EnvPalette", $"{IconUtil.ICON_PALETTE}", flags))
                        {
                            SelectPalette($"Default Palette", area.mAreaParams.EnvPaletteSetting.InitPaletteBaseName);

                            if (area.mAreaParams.EnvPaletteSetting.WonderPaletteList != null)
                            {
                                foreach (var palette in area.mAreaParams.EnvPaletteSetting.WonderPaletteList)
                                    SelectPalette($"Wonder Palette", palette);
                            }
                            if (area.mAreaParams.EnvPaletteSetting.TransPaletteList != null)
                            {
                                foreach (var palette in area.mAreaParams.EnvPaletteSetting.TransPaletteList)
                                    SelectPalette($"Transition Palette", palette);
                            }
                            if (area.mAreaParams.EnvPaletteSetting.EventPaletteList != null)
                            {
                                foreach (var palette in area.mAreaParams.EnvPaletteSetting.EventPaletteList)
                                    SelectPalette($"Event Palette", palette);
                            }
                            ImGui.EndCombo();
                        }

                        ImGui.SameLine();

                        bool useGameShaders = UserSettings.UseGameShaders();
                        if (ImGui.Checkbox("Use Game Shaders", ref useGameShaders))
                        {
                            UserSettings.SetGameShaders(useGameShaders);
                        }

                        ImGui.SameLine();

                        if (ImGui.BeginCombo("Wonder View", viewMode[(int)activeViewport.WonderViewMode], ImGuiComboFlags.WidthFitPreview))
                        {
                            for (int n = 0; n < 3; n++)
                            {
                                if (ImGui.Selectable(viewMode[n]))
                                    viewport.WonderViewMode = (WonderViewType)n;
                            }
                            ImGui.EndCombo();
                        }

                        ImGui.PopStyleColor(1);

                        ImGui.EndChild();
                    }

                    if (ImGui.IsWindowFocused())
                    {
                        if (selectedArea != area)
                        {
                            selectedArea = area;
                            mHasFilledLayers = false;
                        }
                        activeViewport = viewport;
                    }

                    var topLeft = ImGui.GetCursorScreenPos();
                    var size = ImGui.GetContentRegionAvail();

                    ImGui.SetNextItemAllowOverlap();
                    ImGui.SetCursorScreenPos(topLeft);

                    ImGui.SetNextItemAllowOverlap();
                    viewport.Draw(ImGui.GetContentRegionAvail(), deltaSeconds, mLayersVisibility);
                    if (activeViewport != viewport)
                        ImGui.GetWindowDrawList().AddRectFilled(topLeft, topLeft + size, 0x44000000);

                    //Allow button press, align to top of the screen
                    ImGui.SetCursorScreenPos(topLeft);

                    //Load popup when button is pressed
                    if (ImGui.Button("Area Parameters"))
                        ImGui.OpenPopup("AreaParams");

                    ImGui.SameLine();

                    //Display Mouse Position  
                    if (ImGui.IsMouseHoveringRect(topLeft, topLeft + size))
                    {
                        var _mousePos = activeViewport.ScreenToWorld(ImGui.GetMousePos());
                        ImGui.Text("X: " + Math.Round(_mousePos.X, 3) + "\nY: " + Math.Round(_mousePos.Y, 3));
                    }
                    else
                        ImGui.Text("X:\nY:");

                    //Fixed popup pos, render popup
                    //var pos = ImGui.GetCursorScreenPos();
                    //ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
                    AreaParameters(area.mAreaParams);
                }
            }

            if (lastCreatedViewports != viewports)
            {
                for (int i = 0; i < course.GetAreaCount(); i++)
                {
                    var area = course.GetArea(i);
                    var playerLocator = area.mActorHolder.mActors.Find(x => x.mPackName == "PlayerLocator");

                    if (playerLocator is not null)
                    {
                        ImGui.SetWindowFocus(area.GetName());
                        viewports[area].FrameSelectedActor(playerLocator);
                        break;
                    }

                }

                lastCreatedViewports = viewports;
            }

            //minimap.Draw(selectedArea, areaScenes[selectedArea].EditContext, viewports[selectedArea]);

            if (status)
                ImGui.End();
        }

        void UndoHistoryPanel()
        {
          undoWindow.Render(areaScenes[selectedArea].EditContext);
        }
        
        public void Save()
        {
            RSTB resource_table = new RSTB();
            resource_table.Load();

            List<string> pathsToWriteTo = course.GetAreas().Select(
                a=> Path.Combine(UserSettings.GetModRomFSPath(), "BancMapUnit", $"{a.GetName()}.bcett.byml.zs")
                ).ToList();
            pathsToWriteTo.Add(
                Path.Combine(UserSettings.GetModRomFSPath(), "System", "Resource", "ResourceSizeTable.Product.100.rsizetable.zs")
                );

            if (!pathsToWriteTo.All(EnsureFileIsWritable))
            {
                //one or more of the files are locked, due to being open externally. abandon save and show popup informing user
                _ = SaveFailureAlert.ShowDialog(mPopupModalHost);
                return;
            }

            //Save each course area to current romfs folder
            foreach (var area in this.course.GetAreas())
            {
                Console.WriteLine($"Saving area {area.GetName()}...");

                area.Save(resource_table);
            }

            resource_table.Save();
        }

        bool EnsureFileIsWritable(string path)
        {
            if (!File.Exists(path))
                return true;
            try
            {
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    return fs.CanWrite;
                }
            }
            catch(IOException e)
            {
                return false;
            }
        }

        private void ActorsPanel()
        {
            ImGui.Begin("Actors");

            if (ImGui.Button("Add Actor"))
            {
                _ = AddActorsWithSelectActorAndLayerWindow();
            }

            ImGui.SameLine();

            if (ImGui.Button("Delete Actor"))
            {
                var ctx = areaScenes[selectedArea].EditContext;
                var actors = ctx.GetSelectedObjects<CourseActor>().ToList();

                if (actors.Count > 0)
                    _ = DeleteObjectsWithWarningPrompt(actors, 
                        ctx, "Delete actors");
            }

            ImGui.SameLine();

            if (ImGui.Button("Add Layer"))
            {
                _ = AddLayerWithLayerWindow();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text(IconUtil.ICON_SEARCH.ToString());
            ImGui.SameLine();

            ImGui.InputText($"##Search", ref mActorSearchText, 0x100);

            // actors are in an array
            CourseActorHolder actorArray = selectedArea.mActorHolder;

            //CourseActorsTreeView(actorArray);
            CourseActorsLayerView(actorArray);

            ImGui.End();
        }

        private void BGUnitPanel()
        {
            ImGui.Begin("Terrain Units");

            CourseUnitView(selectedArea.mUnitHolder);

            ImGui.End();
        }

        private void RailsPanel()
        {
            ImGui.Begin("Rails");

            CourseRailHolder railArray = selectedArea.mRailHolder;

            CourseRailsView(railArray);

            ImGui.End();
        }

        private void GlobalLinksPanel()
        {
            ImGui.Begin("Global Links");

            if (ImGui.Button("Add Link"))
            {
                course.AddGlobalLink();
            }

            ImGui.Separator();

            CourseGlobalLinksView(course.GetGlobalLinks());

            ImGui.End();
        }
      
        private void LocalLinksPanel()
        {
            ImGui.Begin("Local Links");

            ImGui.Separator();

            AreaLocalLinksView(selectedArea);
            
            ImGui.End();
        }
        
        private void RailLinksPanel()
        {
            ImGui.Begin("Actor to Rail Links");

            var ctx = areaScenes[selectedArea].EditContext;
            var rails = selectedArea.mRailHolder.mRails;
            var actors = selectedArea.mActorHolder.mActors;
            var railLinks = selectedArea.mRailLinksHolder.mLinks;

            if (ImGui.BeginTable("actorRails", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Actor-Hash");
                ImGui.TableSetupColumn("Rail");
                ImGui.TableSetupColumn("Point");
                ImGui.TableHeadersRow();

                for (int i = 0; i < railLinks.Count; i++)
                {
                    ImGui.PushID(i);
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    CourseActorToRailLink link = railLinks[i];

                    string hash = link.mSourceActor.ToString();
                    int actorIndex = actors.FindIndex(x => x.mHash == link.mSourceActor);
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputText("##actor", ref hash, 100) &&
                        ulong.TryParse(hash, out ulong hashInt))
                            link.mSourceActor = hashInt;
                    if(actorIndex == -1)
                    {
                        ImGui.SameLine();
                        ImGui.TextDisabled("Invalid");
                    }
                    ImGui.TableNextColumn();
                    int railIndex = rails.FindIndex(x => x.mHash == link.mDestRail);
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.BeginCombo("##rail", railIndex >= 0 ? ("rail " + railIndex) : "None"))
                    {
                        for (int iRail = 0; iRail < rails.Count; iRail++)
                        {
                            if (ImGui.Selectable("Rail " + iRail, railIndex == iRail))
                                link.mDestRail = rails[iRail].mHash;
                        }
                        ImGui.EndCombo();
                    }
                    if (railIndex == -1)
                    {
                        ImGui.SameLine();
                        ImGui.TextDisabled("Invalid");
                    }
                    ImGui.TableNextColumn();
                    if (railIndex >= 0 && rails[railIndex].mPoints.Count > 0)
                    {
                        int pointIndex = rails[railIndex].mPoints.FindIndex(x => x.mHash == link.mDestPoint);

                        if (pointIndex == -1)
                            pointIndex = 0;

                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        if (ImGui.InputInt("##railpoint", ref pointIndex))
                            pointIndex = Math.Clamp(pointIndex, 0, rails[railIndex].mPoints.Count - 1);

                        link.mDestPoint = rails[railIndex].mPoints[pointIndex].mHash;
                    }

                    ImGui.TableNextColumn();
                    if (ImGui.Button("Delete", new Vector2(ImGui.GetContentRegionAvail().X  - ImGui.GetStyle().ScrollbarSize, 0)))
                    {
                        ctx.DeleteRailLink(link);
                        i--;
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            float width = ImGui.GetItemRectMax().X - ImGui.GetCursorScreenPos().X;

            ImGui.Dummy(new Vector2(0, ImGui.GetFrameHeight() * 0.5f));

            if (ImGui.Button("Add", new Vector2(width, ImGui.GetFrameHeight() * 1.5f)))
            {
                ctx.AddRailLink(new CourseActorToRailLink("Reference"));
            }

            ImGui.End();
        }

        private void SelectionParameterPanel()
        {
            var editContext = areaScenes[selectedArea].EditContext;

            bool status = ImGui.Begin("Selection Parameters", ImGuiWindowFlags.AlwaysVerticalScrollbar);

            if (editContext.IsSingleObjectSelected(out CourseActor? mSelectedActor))
            {
                //invalidate current action if there has been external changes
                if(propertyCapture.capture.HasChangesSinceLastCheckpoint())
                {
                    propertyCapture = (null, FullPropertyCapture.Empty);
                }

                #region Actor UI
                string actorName = mSelectedActor.mPackName;
                string name = mSelectedActor.mName;

                if (ImGui.BeginTable("Props", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                        ImGui.AlignTextToFramePadding();
                        string packName = mSelectedActor.mPackName;

                        ImGui.Text("Actor Name");
                        ImGui.TableNextColumn();
                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                        if (ImGui.InputText("##Actor Name", ref packName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                        {
                            if (ParamDB.GetActors().Contains(packName))
                            {
                                mSelectedActor.mPackName = packName;
                                mSelectedActor.InitializeDefaultDynamicParams();
                            }
                        }
                        ImGui.PopItemWidth();

                    ImGui.TableNextColumn();
                        ImGui.Text("Actor Hash");
                        ImGui.TableNextColumn();
                        string hash = mSelectedActor.mHash.ToString();
                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                        ImGui.InputText("##Actor Hash", ref hash, 256, ImGuiInputTextFlags.ReadOnly);
                        ImGui.PopItemWidth();

                    ImGui.TableNextColumn();
                        ImGui.Separator();
                    ImGui.TableNextColumn();
                        ImGui.Separator();

                    ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Name");

                        ImGui.TableNextColumn();
                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                        if (ImGui.InputText($"##{name}", ref name, 512, ImGuiInputTextFlags.EnterReturnsTrue))
                        {
                            mSelectedActor.mName = name;
                        }

                        ImGui.PopItemWidth();
                    
                    ImGui.TableNextColumn();
                        ImGui.Text("Layer");
                        ImGui.TableNextColumn();
                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                        if (ImGui.BeginCombo("##Dropdown", mSelectedActor.mLayer))
                        {
                            foreach (var layer in mLayersVisibility.Keys.ToArray().ToImmutableList())
                            {
                                if (ImGui.Selectable(layer))
                                {
                                    //item is selected
                                    Console.WriteLine("Changing " + mSelectedActor.mName + "'s layer from " + mSelectedActor.mLayer + " to " + layer + ".");
                                    mSelectedActor.mLayer = layer;
                                }
                            }

                            ImGui.EndCombo();
                        }
                        ImGui.PopItemWidth();

                    ImGui.EndTable();
                }

                PlacementNode(mSelectedActor);

                /* actor parameters are loaded from the dynamic node */
                if (mSelectedActor.mActorParameters.Count > 0)
                {
                    DynamicParamNode(mSelectedActor);
                }

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Links");
                ImGui.Separator();



                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.BeginCombo("##Add Link", "Add Link"))
                {
                    for (int i = 0; i < linkTypes.Length; i++)
                    {
                        var linkType = linkTypes[i];

                        if (ImGui.Selectable(linkType))
                        {
                            KeyboardModifier modifier;
                            ImGui.SetWindowFocus(selectedArea.GetName());
                            Task.Run(async () =>
                            {
                                do
                                {
                                    (var pickedDest, modifier) = await PickLinkDestInViewportFor(mSelectedActor);
                                    if (pickedDest is null)
                                        return;
                                    
                                    var link = new CourseLink(linkType)
                                    {
                                        mSource = mSelectedActor.mHash,
                                        mDest = pickedDest.mHash
                                    };
                                    editContext.AddLink(link);
                                } while ((modifier & KeyboardModifier.Shift) > 0);
                            });
                        }
                    }

                    ImGui.EndCombo();
                }

                var destHashes = selectedArea.mLinkHolder.GetDestHashesFromSrc(mSelectedActor.mHash);

                foreach ((string linkName, List<ulong> hashArray) in destHashes)
                {
                    ImGui.Text(linkName);

                    if (ImGui.BeginTable("##Links", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                        for (int i = 0; i < hashArray.Count; i++)
                        {
                            ImGui.PushID($"{hashArray[i].ToString()}_{i}");
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);
                            ImGui.Text("Destination");
                            ImGui.TableNextColumn();

                            CourseActor? destActor = selectedArea.mActorHolder[hashArray[i]];

                            if (destActor != null)
                            {
                                if (ImGui.Button(destActor.mName, new Vector2(ImGui.GetContentRegionAvail().X, 0)))
                                {
                                    mSelectedActor = destActor;
                                    activeViewport.SelectedActor(destActor);
                                    activeViewport.Camera.Target.X = destActor.mTranslation.X;
                                    activeViewport.Camera.Target.Y = destActor.mTranslation.Y;
                                }
                            }
                            else
                            {
                                if (ImGui.Button("Actor Not Found"))
                                {

                                }
                            }

                            ImGui.TableNextColumn();

                            var cursorSP = ImGui.GetCursorScreenPos();
                            var padding = ImGui.GetStyle().FramePadding;

                            uint WithAlphaFactor(uint color, float factor) => color & 0xFFFFFF | ((uint)((color >> 24) * factor) << 24);

                            float deleteButtonWidth = ImGui.GetFrameHeight() * 1.6f;

                            float columnWidth = ImGui.GetContentRegionAvail().X;

                            ImGui.PushClipRect(cursorSP,
                                cursorSP + new Vector2(columnWidth - deleteButtonWidth, ImGui.GetFrameHeight()), true);

                            var cursor = ImGui.GetCursorPos();
                            ImGui.BeginDisabled();
                            if (ImGui.Button("Replace"))
                            {

                            }
                            ImGui.EndDisabled();
                            cursor.X += ImGui.GetItemRectSize().X + 2;

                            ImGui.SetCursorPos(cursor);
                            if (ImGui.Button(IconUtil.ICON_EYE_DROPPER))
                            {
                                ImGui.SetWindowFocus(selectedArea.GetName());
                                Task.Run(async () =>
                                {
                                    var (pickedDest, _) = await PickLinkDestInViewportFor(mSelectedActor);
                                    if (pickedDest is null)
                                        return;

                                    //TODO rework GetDestHashesFromSrc to return the actual link objects or do it in another way
                                    var link = selectedArea.mLinkHolder.mLinks.Find(
                                        x => x.mSource == mSelectedActor.mHash &&
                                        x.mLinkName == linkName &&
                                        x.mDest == destActor!.mHash);

                                    link.mDest = pickedDest.mHash;
                                });
                            }

                            ImGui.PopClipRect();
                            cursorSP.X += columnWidth - deleteButtonWidth;
                            ImGui.SetCursorScreenPos(cursorSP);

                            bool clicked = ImGui.InvisibleButton("##Delete Link", new Vector2(deleteButtonWidth, ImGui.GetFrameHeight()));
                            string deleteIcon = IconUtil.ICON_TRASH_ALT;
                            ImGui.GetWindowDrawList().AddText(cursorSP + new Vector2((deleteButtonWidth - ImGui.CalcTextSize(deleteIcon).X) / 2, padding.Y),
                                WithAlphaFactor(ImGui.GetColorU32(ImGuiCol.Text), ImGui.IsItemHovered() ? 1 : 0.5f),
                                deleteIcon);

                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip("Delete Link");

                            if (clicked)
                                editContext.DeleteLink(linkName, mSelectedActor.mHash, hashArray[i]);

                            ImGui.PopID();
                            ImGui.TableNextColumn();
                        }
                        ImGui.EndTable();
                    }

                    ImGui.Separator();
                }
                #endregion

                bool needsRecapture = false;

                if (!ImGui.IsAnyItemActive())
                {
                    if (propertyCapture.capture.TryGetRevertable(out var revertable, 
                        names => $"{IconUtil.ICON_WRENCH} Change {string.Join(", ", names)}"))
                    {
                        editContext.CommitAction(revertable);
                        needsRecapture = true;
                    }
                }
                if(needsRecapture || propertyCapture.courseObj != mSelectedActor)
                {
                    propertyCapture = (
                        mSelectedActor,
                        new FullPropertyCapture(mSelectedActor)
                    );
                }

                propertyCapture.capture.MakeCheckpoint();
            }
            else if (editContext.IsSingleObjectSelected(out CourseUnit? mSelectedUnit))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Selected BG Unit");

                ImGui.Separator();

                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginTable("Props", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Model Type"); ImGui.TableNextColumn();

                            ImGui.Combo("##mModelType", ref Unsafe.As<CourseUnit.ModelType, int>(ref mSelectedUnit.mModelType),
                                CourseUnit.ModelTypeNames, CourseUnit.ModelTypeNames.Length);

                        ImGui.TableNextColumn();

                            ImGui.Text("Skin Division"); ImGui.TableNextColumn();
                            ImGui.Combo("##SkinDivision", ref Unsafe.As<CourseUnit.SkinDivision, int>(ref mSelectedUnit.mSkinDivision),
                                CourseUnit.SkinDivisionNames, CourseUnit.SkinDivisionNames.Length);

                        ImGui.EndTable();
                    }
                }

                if(mSelectedUnit.mModelType is CourseUnit.ModelType.SemiSolid)
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    if(ImGui.Button("Remove all Belts"))
                    {
                        var batchAction = editContext.BeginBatchAction();

                        for (int i = mSelectedUnit.mBeltRails.Count - 1; i >= 0; i--)
                            editContext.DeleteBeltRail(mSelectedUnit, mSelectedUnit.mBeltRails[i]);

                        batchAction.Commit("Remove all Belts from TileUnit");
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Generate Belts"))
                    {
                        var batchAction = editContext.BeginBatchAction();

                        void ProcessRail(BGUnitRail rail)
                        {
                            if (rail.Points.Count <= 1)
                                return;

                            BGUnitRail? firstBeltRail = null;
                            BGUnitRail? currentBeltRail = null;

                            var lastPoint = new Vector3(float.NaN);

                            for (int i = 0; i < rail.Points.Count; i++)
                            {
                                var point0 = rail.Points[i].Position;
                                var point1 = rail.Points.GetWrapped(i + 1).Position;

                                if (point0.X >= point1.X)
                                    continue;

                                if (point0 != lastPoint)
                                {
                                    if (currentBeltRail is not null)
                                        editContext.AddBeltRail(mSelectedUnit, currentBeltRail);

                                    currentBeltRail = new BGUnitRail(mSelectedUnit);
                                    currentBeltRail.Points.Add(new BGUnitRail.RailPoint(currentBeltRail, point0));
                                    firstBeltRail ??= currentBeltRail;
                                }

                                currentBeltRail!.Points.Add(new BGUnitRail.RailPoint(currentBeltRail, point1));
                                lastPoint = point1;
                            }

                            var lastBeltRail = currentBeltRail;

                            if(firstBeltRail is not null && lastBeltRail is not null &&
                                firstBeltRail != lastBeltRail &&
                                lastBeltRail.Points[^1].Position == firstBeltRail.Points[0].Position)
                            {
                                //connect first and last rail

                                for (int i = 0; i < lastBeltRail.Points.Count-1; i++)
                                {
                                    var position = lastBeltRail.Points[i].Position;
                                    firstBeltRail.Points.Insert(i, new BGUnitRail.RailPoint(firstBeltRail, position));
                                }
                            }
                            else if (lastBeltRail is not null)
                                editContext.AddBeltRail(mSelectedUnit, lastBeltRail);
                        }

                        foreach (var wall in mSelectedUnit.Walls)
                        {
                            ProcessRail(wall.ExternalRail);

                            foreach (var internalRail in wall.InternalRails)
                                ProcessRail(internalRail);
                        }

                        batchAction.Commit("Add Belts");
                    }
                }
            }
            else if (editContext.IsSingleObjectSelected(out BGUnitRail? mSelectedUnitRail))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Selected BG Unit Rail");

                ImGui.Separator();

                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginTable("Props", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text("IsClosed"); ImGui.TableNextColumn();
                            if (ImGui.Checkbox("##IsClosed", ref mSelectedUnitRail.IsClosed))
                                mSelectedUnitRail.mCourseUnit.GenerateTileSubUnits();

                        ImGui.TableNextColumn();
                            //Depth editing for bg unit. All points share the same depth, so batch edit the Z point
                            float depth = mSelectedUnitRail.Points.Count == 0 ? 0 : mSelectedUnitRail.Points[0].Position.Z;

                            ImGui.Text("Z Depth"); ImGui.TableNextColumn();
                            if (ImGui.DragFloat("##Depth", ref depth, 0.1f))
                            {
                                //Update depth to all points
                                foreach (var p in mSelectedUnitRail.Points)
                                    p.Position = new System.Numerics.Vector3(p.Position.X, p.Position.Y, depth);
                                mSelectedUnitRail.mCourseUnit.GenerateTileSubUnits();
                            }
                   
                        ImGui.EndTable();
                    }
                }
            }
            else if (editContext.IsSingleObjectSelected(out CourseLink? mSelectedGlobalLink))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Selected Global Link");
                ImGui.NewLine();

                if (ImGui.Button("Delete Link"))
                {
                    course.RemoveGlobalLink(mSelectedGlobalLink);
                    mSelectedGlobalLink = null;
                    return;
                }

                ImGui.Separator();

                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginTable("Props", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Source Hash"); ImGui.TableNextColumn();
                            string srcHash = mSelectedGlobalLink.mSource.ToString();
                            if (ImGui.InputText("##Source Hash", ref srcHash, 256, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue))
                            {
                                mSelectedGlobalLink.mSource = Convert.ToUInt64(srcHash);
                            }

                        ImGui.TableNextColumn();
                            ImGui.Text("Destination Hash"); ImGui.TableNextColumn();
                            string destHash = mSelectedGlobalLink.mDest.ToString();
                            if (ImGui.InputText("##Dest Hash", ref destHash, 256, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue))
                            {
                                mSelectedGlobalLink.mDest = Convert.ToUInt64(destHash);
                            }

                        ImGui.TableNextColumn();
                            ImGui.Text("Link Type"); ImGui.TableNextColumn();

                            List<string> types = linkTypes.ToList();
                            int idx = types.IndexOf(mSelectedGlobalLink.mLinkName);
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.Combo("##Link Type", ref idx, linkTypes, linkTypes.Length))
                            {
                                mSelectedGlobalLink.mLinkName = linkTypes[idx];
                            }

                        ImGui.EndTable();
                    }
                }
            }
            else if (editContext.IsSingleObjectSelected(out CourseRail? mSelectedRail))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Selected Rail");
                ImGui.NewLine();
                ImGui.Separator();

                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginTable("DynamProps", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Hash"); ImGui.TableNextColumn();
                            string hash = mSelectedRail.mHash.ToString();
                            if (ImGui.InputText("##Hash", ref hash, 256, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue))
                            {
                                mSelectedRail.mHash = Convert.ToUInt64(hash);
                            }

                        ImGui.TableNextColumn();
                            ImGui.Text("IsClosed");
                            ImGui.TableNextColumn();
                            ImGui.Checkbox("##IsClosed", ref mSelectedRail.mIsClosed);

                        ImGui.EndTable();
                    }
                }

                if (ImGui.CollapsingHeader("Dynamic Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginTable("DynamProps", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        foreach (KeyValuePair<string, object> param in mSelectedRail.mParameters)
                        {
                            string type = param.Value.GetType().ToString();
                            ImGui.Text(param.Key);
                            ImGui.TableNextColumn();

                            switch (type)
                            {
                                case "System.Int32":
                                    int int_val = (int)param.Value;
                                    if (ImGui.InputInt($"##{param.Key}", ref int_val))
                                    {
                                        mSelectedRail.mParameters[param.Key] = int_val;
                                    }
                                    break;
                                case "System.Boolean":
                                    bool bool_val = (bool)param.Value;
                                    if (ImGui.Checkbox($"##{param.Key}", ref bool_val))
                                    {
                                        mSelectedRail.mParameters[param.Key] = bool_val;
                                    }
                                    break;
                            }
                            ImGui.TableNextColumn();
                        }
                        ImGui.EndTable();
                    }
                }
            }
            else if (editContext.IsSingleObjectSelected(out CourseRail.CourseRailPoint? mSelectedRailPoint) ||
                editContext.IsSingleObjectSelected(out CourseRail.CourseRailPointControl? mSelectedRailPointCont))
            {
                if(editContext.IsSingleObjectSelected(out CourseRail.CourseRailPointControl? cont))
                    mSelectedRailPoint ??= cont.point;
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Selected Rail Point");
                ImGui.NewLine();
                ImGui.Separator();
                
                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginTable("Props", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Hash"); ImGui.TableNextColumn();
                            string hash = mSelectedRailPoint.mHash.ToString();
                            if (ImGui.InputText("##Hash", ref hash, 256, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue))
                            {
                                mSelectedRailPoint.mHash = Convert.ToUInt64(hash);
                            }

                        ImGui.TableNextColumn();
                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("Translation");
                            
                            ImGui.TableNextColumn();

                            ImGui.DragFloat3("##Translation", ref mSelectedRailPoint.mTranslate, 0.25f);

                        ImGui.TableNextColumn();
                            ImGui.AlignTextToFramePadding();
                            ImGui.Text("Curve Control");
                            
                            ImGui.TableNextColumn();
                            ImGui.Checkbox("##Curved", ref mSelectedRailPoint.mIsCurve);
                            ImGui.SameLine();

                            ImGui.BeginDisabled(!mSelectedRailPoint.mIsCurve);

                            ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                            ImGui.DragFloat3("##Control", ref mSelectedRailPoint.mControl.mTranslate, 0.25f);  
                            ImGui.PopItemWidth();

                            ImGui.EndDisabled();

                        ImGui.EndTable();
                    }
                }

                if (ImGui.CollapsingHeader("Dynamic Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginTable("DynamProps", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                        foreach (KeyValuePair<string, object> param in mSelectedRailPoint.mParameters)
                        {
                            string type = param.Value.GetType().ToString();
                            ImGui.Text(param.Key);
                            ImGui.TableNextColumn();

                            switch (type)
                            {
                                case "System.UInt32":
                                    int uint_val = Convert.ToInt32(param.Value);
                                    if (ImGui.InputInt($"##{param.Key}", ref uint_val))
                                    {
                                        mSelectedRailPoint.mParameters[param.Key] = Convert.ToUInt32(uint_val);
                                    }
                                    break;
                                case "System.Int32":
                                    int int_val = (int)param.Value;
                                    if (ImGui.InputInt($"##{param.Key}", ref int_val))
                                    {
                                        mSelectedRailPoint.mParameters[param.Key] = int_val;
                                    }
                                    break;
                                case "System.Single":
                                    float float_val = (float)param.Value;
                                    if (ImGui.InputFloat($"##{param.Key}", ref float_val))
                                    {
                                        mSelectedRailPoint.mParameters[param.Key] = float_val;
                                    }
                                    break;
                                case "System.Boolean":
                                    bool bool_val = (bool)param.Value;
                                    if (ImGui.Checkbox($"##{param.Key}", ref bool_val))
                                    {
                                        mSelectedRailPoint.mParameters[param.Key] = bool_val;
                                    }
                                    break;
                            }
                            ImGui.TableNextColumn();
                        }
                        ImGui.EndTable();
                    }
                }
            }
            else
            {
                ImGui.AlignTextToFramePadding();

                string text = "No item selected";

                var windowWidth = ImGui.GetWindowSize().X;
                var textWidth = ImGui.CalcTextSize(text).X;

                var windowHight = ImGui.GetWindowSize().Y;
                var textHeight = ImGui.CalcTextSize(text).Y;

                ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
                ImGui.SetCursorPosY((windowHight - textHeight) * 0.5f);
                ImGui.BeginDisabled();
                ImGui.Text(text);
                ImGui.EndDisabled();
            }

            if (status)
            {
                ImGui.End();
            }
        }

        private static void AreaParameters(AreaParam area)
        {
            ParamHolder areaParams = ParamLoader.GetHolder("AreaParam");
            var pos = ImGui.GetCursorScreenPos();
            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
            ImGui.SetNextWindowContentSize(new Vector2(400, 800));

            if (ImGui.BeginPopup($"AreaParams", ImGuiWindowFlags.NoMove))
            {
                ImGui.SeparatorText("Area Parameters");

                if (ImGui.BeginTable("AreaParms", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    foreach (string key in areaParams.Keys)
                    {
                        
                        string paramType = areaParams[key];

                        //if (!area.ContainsParam(key))
                        //{
                        //    continue;
                        //}

                        ImGui.Text(key);
                        ImGui.TableNextColumn();

                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - 5);

                        switch (paramType)
                        {
                            case "String":
                                {
                                    string value = "";
                                    if (area.ContainsParam(key))
                                    {
                                        value = (string)area.GetParam(area.GetRoot(), key, paramType);
                                    }
                                    ImGui.InputText($"##{key}", ref value, 1024);
                                    break;
                                }
                            case "Bool":
                                {
                                    bool value = false;
                                    if (area.ContainsParam(key))
                                    {
                                        value = (bool)area.GetParam(area.GetRoot(), key, paramType);
                                    }
                                    ImGui.Checkbox($"##{key}", ref value);
                                    break;
                                }
                            case "Int":
                                {
                                    int value = 0;
                                    if (area.ContainsParam(key))
                                    {
                                        //value = (int)area.GetParam(area.GetRoot(), key, paramType);
                                    }
                                    ImGui.InputInt($"##{key}", ref value);
                                    break;
                                }
                            case "Float":
                                {
                                    float value = 0.0f;
                                    if (area.ContainsParam(key))
                                    {
                                        value = (float)area.GetParam(area.GetRoot(), key, paramType);
                                    }
                                    ImGui.InputFloat($"##{key}", ref value);
                                    break;
                                }
                            default:
                                Console.WriteLine(key);
                                break;
                        }
                        ImGui.PopItemWidth();
                        ImGui.TableNextColumn();
                    }
                    ImGui.EndTable();
                }
                ImGui.EndPopup();
            }
        }

        private void FillLayers(CourseActorHolder actorArray)
        {
            mLayersVisibility.Clear();
            foreach (CourseActor actor in actorArray.mActors)
            {
                string actorLayer = actor.mLayer;
                mLayersVisibility[actorLayer] = true;
            }

            mHasFilledLayers = true;
        }

        private void CourseUnitView(CourseUnitHolder unitHolder)
        {
            var editContext = areaScenes[selectedArea].EditContext;

            BGUnitRailSceneObj GetRailSceneObj(object courseObject)
            {
                if (!areaScenes[selectedArea].TryGetObjFor(courseObject, out var sceneObj))
                    throw new Exception("Couldn't find scene object");
                return (BGUnitRailSceneObj)sceneObj;
            }

            ImGui.Text("Select a Wall");
            ImGui.Text("Alt + Left Click to add point");

            if (ImGui.Button("Add Tile Unit", new Vector2(100, 22)))
            {
                editContext.AddBgUnit(new CourseUnit());
            }

            List<CourseUnit> removed_tile_units = new List<CourseUnit>();

            foreach (var unit in unitHolder.mUnits)
            {
                var tree_flags = ImGuiTreeNodeFlags.None;
                string name = $"Tile Unit {unitHolder.mUnits.IndexOf(unit)}";

                ImGui.AlignTextToFramePadding();
                bool expanded = ImGui.TreeNodeEx($"##{name}", ImGuiTreeNodeFlags.DefaultOpen);

                ImGui.SameLine();
                ImGui.SetNextItemAllowOverlap();
                if (ImGui.Checkbox($"##Visible{name}", ref unit.Visible))
                {
                    foreach (var wall in unit.Walls)
                    {
                        GetRailSceneObj(wall.ExternalRail).Visible = unit.Visible;
                        foreach (var rail in wall.InternalRails)
                            GetRailSceneObj(rail).Visible = unit.Visible;
                    }
                }
                ImGui.SameLine();

                if (ImGui.Selectable(name, editContext.IsSelected(unit)))
                {
                    editContext.DeselectAll();
                    editContext.Select(unit);
                }
                if (expanded)
                {
                    void RailListItem(string type, BGUnitRail rail, int id)
                    {
                        bool isSelected = editContext.IsSelected(rail);
                        string wallname = $"{type} {id}";

                        ImGui.Indent();

                        if (ImGui.Checkbox($"##Visible{wallname}", ref GetRailSceneObj(rail).Visible))
                        {

                        }
                        ImGui.SameLine();

                        if (ImGui.BeginTable("Rails", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                        {
                            ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);

                                void SelectRail()
                                {
                                    editContext.DeselectAll();
                                    editContext.Select(rail);
                                }

                                if (ImGui.Selectable($"##{name}{wallname}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                                {
                                    SelectRail();
                                }
                                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                                {
                                    SelectRail();
                                }

                                ImGui.SameLine();

                                //Shift text from selection
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 22);
                                ImGui.Text(wallname);

                            ImGui.TableNextColumn();

                                ImGui.TextDisabled($"(Num Points: {rail.Points.Count})");

                            ImGui.EndTable();
                        }

                        ImGui.Unindent();
                    }

                    if (editContext.IsSelected(unit))
                    {
                        if (ImGui.BeginPopupContextWindow("RailMenu", ImGuiPopupFlags.MouseButtonRight))
                        {
                            if (ImGui.MenuItem("Add Wall"))
                                editContext.AddWall(unit, new Wall(unit));

                            if (ImGui.MenuItem($"Remove {name}"))
                                removed_tile_units.Add(unit);

                            ImGui.EndPopup();
                        }
                    }

                    if (unit.mModelType is not CourseUnit.ModelType.Bridge)
                    {
                        if (ImGui.Button("Add Wall"))
                            editContext.AddWall(unit, new Wall(unit));
                        ImGui.SameLine();
                        if (ImGui.Button("Remove Wall"))
                        {
                            editContext.WithSuspendUpdateDo(() =>
                            {
                                for (int i = unit.Walls.Count - 1; i >= 0; i--)
                                {
                                    //TODO is that REALLY how we want to do this?
                                    if (editContext.IsSelected(unit.Walls[i].ExternalRail))
                                        editContext.DeleteWall(unit, unit.Walls[i]);
                                }
                            });
                        }

                        for (int iWall = 0; iWall < unit.Walls.Count; iWall++)
                        {
                            Wall wall = unit.Walls[iWall];
                            if (wall.InternalRails.Count > 0)
                            {
                                ImGui.Unindent();
                                bool ex = ImGui.TreeNodeEx($"##{name}Wall{iWall}", ImGuiTreeNodeFlags.DefaultOpen);
                                ImGui.SameLine();

                                RailListItem("Wall", wall.ExternalRail, unit.Walls.IndexOf(wall));

                                ImGui.Indent();

                                if (ex)
                                {
                                    for (int iInternal = 0; iInternal < wall.InternalRails.Count; iInternal++)
                                    {
                                        BGUnitRail? rail = wall.InternalRails[iInternal];
                                        RailListItem("Internal Rail", rail, iInternal);
                                    }
                                }

                                ImGui.TreePop();
                            }
                            else
                            {
                                RailListItem("Wall", wall.ExternalRail, iWall);
                            }
                        }
                    } 

                    if (unit.mModelType is CourseUnit.ModelType.SemiSolid or CourseUnit.ModelType.Bridge)
                    {
                        if (ImGui.Button("Add Belt"))
                            editContext.AddBeltRail(unit, new BGUnitRail(unit));
                        ImGui.SameLine();
                        if (ImGui.Button("Remove Belt"))
                        {
                            editContext.WithSuspendUpdateDo(() =>
                            {
                                for (int i = unit.mBeltRails.Count - 1; i >= 0; i--)
                                {
                                    if (editContext.IsSelected(unit.mBeltRails[i]))
                                        editContext.DeleteBeltRail(unit, unit.mBeltRails[i]);
                                }
                            });
                        }

                        for (int iBeltRail = 0; iBeltRail < unit.mBeltRails.Count; iBeltRail++)
                        {
                            BGUnitRail beltRail = unit.mBeltRails[iBeltRail];
                            RailListItem("Belt", beltRail, iBeltRail);
                        }
                    }
                    ImGui.TreePop();
                }
            }

            if (removed_tile_units.Count > 0)
            {
                foreach (var tile in removed_tile_units)
                    editContext.DeleteBgUnit(tile);
                removed_tile_units.Clear();
            }
        }

        private void CourseRailsView(CourseRailHolder railHolder)
        {
            var editContext = areaScenes[selectedArea].EditContext;

            if (ImGui.Button("Add Rail"))
            {
                railHolder.mRails.Add(new CourseRail(this.selectedArea.mRootHash));
            }
            ImGui.SameLine();
            if (ImGui.Button("Remove Rail"))
            {
                var selected = editContext.GetSelectedObjects<CourseRail>();
                foreach (var rail in selected)
                    railHolder.mRails.Remove(rail);
            }

            foreach (CourseRail rail in railHolder.mRails)
            {
                var rail_node_flags = ImGuiTreeNodeFlags.OpenOnArrow;
                if (editContext.IsSelected(rail) &&
                    !editContext.IsAnySelected<CourseRail.CourseRailPoint>())
                {
                    rail_node_flags |= ImGuiTreeNodeFlags.Selected;
                }

                bool expanded = ImGui.TreeNodeEx($"Rail {railHolder.mRails.IndexOf(rail)}", rail_node_flags);
                if (ImGui.IsItemHovered(0) && ImGui.IsMouseClicked(0))
                {
                    editContext.DeselectAll();
                    editContext.Select(rail);
                }

                if (expanded)
                {
                    foreach (CourseRail.CourseRailPoint pnt in rail.mPoints)
                    {
                        var flags = ImGuiTreeNodeFlags.Leaf;
                        if (editContext.IsSelected(pnt))
                            flags |= ImGuiTreeNodeFlags.Selected;

                        if (ImGui.TreeNodeEx($"Point {rail.mPoints.IndexOf(pnt)}", flags))
                            ImGui.TreePop();

                        if (ImGui.IsItemHovered(0) && ImGui.IsMouseClicked(0))
                        {
                            editContext.DeselectAll();
                            editContext.Select(pnt);
                        }
                    }

                    ImGui.TreePop();
                }
            }
        }

        private void CourseGlobalLinksView(CourseLinkHolder linkHolder)
        {
            var editContext = areaScenes[selectedArea].EditContext;
            for (int i = 0; i < linkHolder.mLinks.Count; i++)
            {
                CourseLink link = linkHolder.mLinks[i];
                if (ImGui.Selectable($"Link {i}", editContext.IsSelected(link)))
                {
                    editContext.DeselectAll();
                    editContext.Select(link);
                }
            }
        }
        
        //VERY ROUGH BASE
        //TODO, optomize recursion
        List<CourseActor> topLinks;
        CourseActor? selected;
        private void AreaLocalLinksView(CourseArea area)
        {
            var links = area.mLinkHolder;
            var editContext = areaScenes[selectedArea].EditContext;

            float em = ImGui.GetFrameHeight();
            var wcMin = ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetScrollY());
            var wcMax = wcMin + ImGui.GetContentRegionAvail();

            topLinks = area.GetActors()
                .Where(x => links.mLinks.Any(y => y.mSource == x.mHash)).ToList();
            if (ImGui.BeginTable("##Links", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
            {
                CourseActor? selected = null;
                RecursiveLinkFind(area, links, editContext, em, topLinks, []);
                ImGui.EndTable();
            }

            ImGui.PopClipRect();

            ImGui.EndChild();
        }

        private void RecursiveLinkFind(CourseArea area, CourseLinkHolder links, 
            CourseAreaEditContext editContext, float em, IEnumerable<CourseActor> linkList,
            Hashtable parentActors)
        {
            foreach (CourseActor actor in linkList)
            {
                var destLinks = links.GetDestHashesFromSrc(actor.mHash);
                ImGui.TableNextRow();

                string actorName = actor.mPackName;
                string name = actor.mName;
                ulong actorHash = actor.mHash;
                bool isSelected = editContext.IsSelected(actor);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextDisabled(name);

                bool expanded = false;
                bool isVisible = true;
                float margin = 1.5f * em;
                float headerHeight = 1.4f * em;
                Vector2 cp = ImGui.GetCursorScreenPos();
                ImGui.TableSetColumnIndex(0);

                ImGuiTreeNodeFlags node_flags = ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.OpenOnArrow;

                if (isSelected)
                    node_flags |= ImGuiTreeNodeFlags.Selected;

                if (!parentActors.ContainsValue(actor) && destLinks.Count > 0)
                    expanded = ImGui.TreeNodeEx($"{actorHash}", node_flags, actorName);
                else
                    expanded = ImGui.Selectable(actorName, isSelected);

                if (ImGui.IsItemClicked())
                {
                    activeViewport.SelectedActor(actor);
                }

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                {
                    activeViewport.FrameSelectedActor(actor);
                    selected ??= actor;
                }
                if (parentActors.Count == 0 && selected == actor)
                {
                    ImGui.SetScrollHereY();
                    selected = null;
                }

                ImGui.BeginDisabled(!isVisible);

                UpdateWonderVisibility(actor, destLinks, area);

                if (expanded)
                {
                    if(!parentActors.ContainsValue(actor) && destLinks.Count > 0)
                    {
                        foreach (var link in destLinks)
                        {
                            if(ImGui.TreeNodeEx($"{link.Key}##{actorHash}", ImGuiTreeNodeFlags.FramePadding, link.Key))
                            {
                                var parents = new Hashtable(parentActors);
                                parents[actorHash] = actor;
                                var reLinks = area.GetActors().Where(x => link.Value.Contains(x.mHash));
                                RecursiveLinkFind(area, links, editContext, em, reLinks, parents);
                                ImGui.TreePop();
                            }
                        }
                        ImGui.TreePop();
                    }                 
                }
            
                ImGui.EndDisabled();
            }
            parentActors.Clear();
        }

        static void UpdateWonderVisibility(CourseActor actor, Dictionary<string, List<ulong>> links, CourseArea area)
        {
            foreach (var link in links)
            {
                var reLinks = area.GetActors().Where(x => link.Value.Contains(x.mHash));
                if (!link.Key.Contains("CreateRelative") &&
                    (link.Key.Contains("Create") ||
                    link.Key.Contains("PopUp") || 
                    link.Key.Contains("Delete") ||
                    link.Key.Contains("BasicSignal")))
                {
                    foreach (CourseActor linkActor in reLinks)
                    {
                        if ((actor.mPackName == "ObjectWonderTag" || actor.mWonderView == WonderViewType.WonderOnly) &&
                        (!link.Key.Contains("BasicSignal") || (linkActor.mActorPack?.Category.Contains("Tag") ?? false)))
                        {
                            if (link.Key.Contains("Delete"))
                                linkActor.mWonderView = WonderViewType.WonderOff;
                            else
                                linkActor.mWonderView = WonderViewType.WonderOnly;
                        }
                        else
                            linkActor.mWonderView = WonderViewType.Normal;
                    }
                }
            }
        }

        private void UpdateAllLayerVisiblity()
        {
            foreach (string layer in mLayersVisibility.Keys)
            {
                mLayersVisibility[layer] = mAllLayersVisible;
            }
        }

        private static bool ToggleButton(string id, string textOn, string textOff, ref bool value, Vector2 size = default)
        {
            var textOnSize = ImGui.CalcTextSize(textOn) * 1.2f;
            var textOffSize = ImGui.CalcTextSize(textOff) * 1.2f;

            if (size.X <= 0 || size.Y <= 0)
            {

                size.X = MathF.Max(textOffSize.X, textOnSize.X) + ImGui.GetStyle().FramePadding.X * 2;
                size.Y = MathF.Max(textOffSize.Y, textOnSize.Y) + ImGui.GetStyle().FramePadding.Y * 2;
            }

            Vector2 cp = ImGui.GetCursorScreenPos();
            bool clicked = ImGui.InvisibleButton(id, size);
            if (clicked)
                value = !value;

            float alpha = value ? 1f : 0.5f;

            if (!ImGui.IsItemHovered())
                alpha -= 0.2f;

            ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), ImGui.GetFontSize() * 1.2f,
                cp + (size - (value ? textOnSize : textOffSize)) / 2,
                (ImGui.GetColorU32(ImGuiCol.Text) & 0xFF_FF_FF) | (uint)(0xFF * alpha) << 24,
                value ? textOn : textOff
                );

            return clicked;
        }

        private void CourseActorsLayerView(CourseActorHolder actorArray)
        {
            var editContext = areaScenes[selectedArea].EditContext;

            float em = ImGui.GetFrameHeight();

            if (!mHasFilledLayers)
            {
                FillLayers(actorArray);
            }

            float margin = 1.5f * em;

            float headerHeight = 1.4f * em;
            Vector2 cp = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(
                cp,
                cp + new Vector2(ImGui.GetContentRegionAvail().X, headerHeight),
                ImGui.GetColorU32(ImGuiCol.FrameBg));
            ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), em * 0.9f,
                cp + new Vector2(em, (headerHeight - em) / 2 + 0.05f), 0xFF_FF_FF_FF,
                "Layers");

            var wcMin = ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetScrollY());
            var wcMax = wcMin + ImGui.GetContentRegionAvail();

            ImGui.SetCursorScreenPos(new Vector2(wcMax.X - margin, cp.Y + (headerHeight - em) / 2));
            if (ToggleButton($"VisibleCheckbox All", IconUtil.ICON_EYE, IconUtil.ICON_EYE_SLASH,
                ref mAllLayersVisible, new Vector2(em)))
                UpdateAllLayerVisiblity();

            ImGui.SetCursorScreenPos(cp + new Vector2(0, headerHeight));

            ImGui.BeginChild("Layers");

            wcMin = ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetScrollY());
            wcMax = wcMin + ImGui.GetContentRegionAvail();

            ImGui.PushClipRect(wcMin, wcMax - new Vector2(margin, 0), true);

            bool isSearch = !string.IsNullOrWhiteSpace(mActorSearchText);
            //var sortedLayers = mLayersVisibility.Keys.ToList();
            //sortedLayers.Sort(layerSort);

            ImGui.Spacing();
            foreach (string layer in mLayersVisibility.Keys) //Use sortedLayers if you think the sorting code is good
            {
                ImGui.PushID(layer);
                cp = ImGui.GetCursorScreenPos();
                bool expanded = false;
                bool isVisible = true;

                if (!isSearch)
                {
                    expanded = ImGui.TreeNodeEx("TreeNode", ImGuiTreeNodeFlags.FramePadding, layer);

                    ImGui.PushClipRect(wcMin, wcMax, false);
                    ImGui.SetCursorScreenPos(new Vector2(wcMax.X - (margin + em*4) / 2, cp.Y));
                    isVisible = mLayersVisibility[layer];
                    if (ToggleButton($"VisibleCheckbox", IconUtil.ICON_EYE, IconUtil.ICON_EYE_SLASH,
                        ref isVisible, new Vector2(em)))
                        mLayersVisibility[layer] = isVisible;
                    ImGui.PopClipRect();
                }
                else
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(layer);
                }
                var dummy = false;
                ImGui.PushClipRect(wcMin, wcMax, false);
                ImGui.SetCursorScreenPos(new Vector2(wcMax.X - (margin + em) / 2, cp.Y));
                if (ToggleButton($"Delete Layer", IconUtil.ICON_TRASH, IconUtil.ICON_TRASH,
                    ref dummy, new Vector2(em)))
                    _ = DeleteLayerWithWarningPrompt(layer, actorArray, editContext);
                ImGui.PopClipRect();

                ImGui.BeginDisabled(!isVisible);

                if (expanded || isSearch)
                {
                    foreach (CourseActor actor in actorArray.mActors)
                    {
                        string actorName = actor.mPackName;
                        string name = actor.mName;
                        ulong actorHash = actor.mHash;
                        string actorLayer = actor.mLayer;

                        //Check if the node is within the necessary search filter requirements if search is used
                        bool HasText = actor.mName.IndexOf(mActorSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       actor.mPackName.IndexOf(mActorSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       actorHash.ToString().Equals(mActorSearchText);

                        if (isSearch && !HasText)
                            continue;

                        if (actorLayer != layer)
                        {
                            continue;
                        }

                        bool isSelected = editContext.IsSelected(actor);

                        ImGui.PushID(actorHash.ToString());
                        if (ImGui.BeginTable("##Links", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                        {
                            ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);
                        
                            if (ImGui.Selectable(actorName, isSelected, ImGuiSelectableFlags.SpanAllColumns))
                            {
                                activeViewport.SelectedActor(actor);
                            }
                            else if (ImGui.IsItemFocused())
                            {
                                activeViewport.SelectedActor(actor);
                            }

                            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                            {
                                activeViewport.FrameSelectedActor(actor);
                            }


                            ImGui.TableNextColumn();
                            ImGui.BeginDisabled();
                            ImGui.Text(name);
                            ImGui.EndDisabled();
                            ImGui.EndTable();
                        }

                        ImGui.PopID();
                    }

                    if (!isSearch)
                        ImGui.TreePop();
                }

                ImGui.EndDisabled();

                ImGui.PopID();
            }

            ImGui.PopClipRect();

            ImGui.EndChild();
        }

        private void CourseMiniView()
        {
            var area = selectedArea;
            var editContext = areaScenes[area].EditContext;
            var view = viewports[area];
            bool status = ImGui.Begin("Minimap", ImGuiWindowFlags.NoNav);

            var widgetTopLeft = ImGui.GetCursorScreenPos();

            var widgetSize = ImGui.GetContentRegionAvail();
            ImGui.InvisibleButton("MiniMapWidget", widgetSize);
            bool isActive = ImGui.IsItemActive();
            bool isHovered = ImGui.IsItemHovered();

            var cam = view.Camera;
            var camSize = view.GetCameraSizeIn2DWorldSpace();

            BoundingBox2D bb = BoundingBox2D.Empty;

            foreach (var actor in area.GetActors())
            {
                if (actor.mPackName == "GlobalAreaInfoActor")
                    continue;

                bb.Include(new Vector2(actor.mTranslation.X, actor.mTranslation.Y));
            }

            foreach (var unit in area.mUnitHolder.mUnits)
            {
                foreach (var subUnit in unit.mTileSubUnits)
                {
                    var origin2D = new Vector2(subUnit.mOrigin.X, subUnit.mOrigin.Y);

                    foreach (var tile in subUnit.GetTiles(new Vector2(float.NegativeInfinity), new Vector2(float.PositiveInfinity)))
                    {
                        var pos = tile.pos + origin2D;
                        bb.Include(new BoundingBox2D(pos, pos + Vector2.One));
                    }
                }
            }
            var levelSize = bb.Max - bb.Min;

            var ratio = widgetSize.X/levelSize.X < widgetSize.Y/levelSize.Y ? widgetSize.X/levelSize.X : widgetSize.Y/levelSize.Y;
            var lvlRectSize = levelSize*ratio;
            var miniCamPos = new Vector2(cam.Target.X - bb.Min.X, -cam.Target.Y + bb.Min.Y) * ratio;
            var miniCamSize = camSize*ratio;
            var miniCamSave = new Vector2(camSave.X - bb.Min.X, -camSave.Y + bb.Min.Y) * ratio;
            var padding = (widgetSize - lvlRectSize)/2;

            var lvlRectTopLeft = widgetTopLeft + padding;

            var col = ImGuiCol.ButtonActive;

            if (ImGui.IsMouseDown(ImGuiMouseButton.Right) && !ImGui.IsMouseDown(ImGuiMouseButton.Left) &&
            (isHovered || (isActive && ImGui.IsMouseReleased(ImGuiMouseButton.Left))) && camSave == default)
            {
                camSave = cam.Target;
            }

            if ((ImGui.IsMouseDown(ImGuiMouseButton.Left) ||
            ImGui.IsMouseDown(ImGuiMouseButton.Right)) &&
            isHovered)
            {
                if (camSave != default)
                {
                    col = ImGuiCol.TextDisabled;
                    ImGui.GetWindowDrawList().AddRect(lvlRectTopLeft + miniCamSave - miniCamSize/2 + new Vector2(0, lvlRectSize.Y), 
                        lvlRectTopLeft + miniCamSave + miniCamSize/2 + new Vector2(0, lvlRectSize.Y), 
                        ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Button]),6,0,3);
                }

                var pos = ImGui.GetMousePos();
                cam.Target = new((pos.X - lvlRectTopLeft.X)/ratio + bb.Min.X,
                    (-pos.Y + lvlRectTopLeft.Y + lvlRectSize.Y)/ratio + bb.Min.Y, cam.Target.Z);
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && camSave != default)
            {
                if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    cam.Target = camSave;

                camSave = default;
            }

            var dl = ImGui.GetWindowDrawList();

            Vector2 MapPointPixelAligned(Vector2 pos) => new Vector2(
                MathF.Round(lvlRectTopLeft.X + (pos.X - bb.Min.X) / (bb.Max.X - bb.Min.X) * lvlRectSize.X),
                MathF.Round(lvlRectTopLeft.Y + (pos.Y - bb.Max.Y) / (bb.Min.Y - bb.Max.Y) * lvlRectSize.Y)
                );

            var backgroundSubUnits = area.mUnitHolder.mUnits
                .Where(x => x.mModelType == CourseUnit.ModelType.NoCollision)
                .SelectMany(x => x.mTileSubUnits);

            foreach (var subUnit in backgroundSubUnits)
            {
                var origin2D = new Vector2(subUnit.mOrigin.X, subUnit.mOrigin.Y);

                foreach (var tile in subUnit.GetTiles(bb.Min - origin2D, bb.Max - origin2D))
                {
                    var pos = tile.pos + origin2D;
                    dl.AddRectFilled(
                        MapPointPixelAligned(pos),
                        MapPointPixelAligned(pos + Vector2.One),
                        0xFF666688);
                }
            }

            var foregroundTileUnits = area.mUnitHolder.mUnits
                .Where(x => x.mModelType != CourseUnit.ModelType.NoCollision);

            var foregroundSubUnits = foregroundTileUnits
                .SelectMany(x => x.mTileSubUnits)
                .OrderBy(x => x.mOrigin.Z);

            foreach (var subUnit in foregroundSubUnits)
            {
                var type = foregroundTileUnits.First(x => x.mTileSubUnits.Contains(subUnit)).mModelType;
                var unitColor = 0xFF999999;
                var edgeColor = 0xFFEEEEEE;

                switch (type)
                {
                    case CourseUnit.ModelType.Solid:
                        unitColor = 0xFFBB9999; 
                        edgeColor = 0xFFFFEEEE;
                        break;
                    case CourseUnit.ModelType.SemiSolid:
                        unitColor = 0xFF99BB99; 
                        edgeColor = 0xFFEEFFEE;
                        break;
                }

                var origin2D = new Vector2(subUnit.mOrigin.X, subUnit.mOrigin.Y);
                foreach (var tile in subUnit.GetTiles(bb.Min - origin2D, bb.Max - origin2D))
                {
                    var pos = tile.pos + origin2D;
                    dl.AddRectFilled(
                        MapPointPixelAligned(pos),
                        MapPointPixelAligned(pos + Vector2.One),
                        unitColor);
                }
                if (subUnit == foregroundSubUnits.Last(x => x.mOrigin.Z == subUnit.mOrigin.Z))
                {
                    foreach(var wall in foregroundTileUnits
                        .SelectMany(x => x.Walls)
                        .Where(x => x.ExternalRail.Points.FirstOrDefault()?.Position.Z == subUnit.mOrigin.Z))
                    {
                        var rail = wall.ExternalRail;

                        var pos = rail.Points.Select(x => MapPointPixelAligned(new(x.Position.X, x.Position.Y))).ToArray();
                        dl.AddPolyline(ref pos[0], 
                            rail.Points.Count, 
                            edgeColor, 
                            rail.IsClosed ? ImDrawFlags.Closed:ImDrawFlags.None, 
                            1.5f);
                    }
                }
            }

            dl.AddRect(lvlRectTopLeft, 
                lvlRectTopLeft + lvlRectSize, 
                ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]),6,0,3);

            dl.AddRect(lvlRectTopLeft + miniCamPos - miniCamSize/2 + new Vector2(0, lvlRectSize.Y), 
                lvlRectTopLeft + miniCamPos + miniCamSize/2 + new Vector2(0, lvlRectSize.Y), 
                ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)col]),6,0,3);

            if (status)
                ImGui.End();
            
        }

        private static void PlacementNode(CourseActor actor)
        {
            static void EditFloat3RadAsDeg(string label, ref System.Numerics.Vector3 rad, float speed)
            {
                float RadToDeg(float rad)
                {
                    double deg = 180 / Math.PI * rad;
                    return (float)deg;
                }

                float DegToRad(float deg)
                {
                    double rad = Math.PI / 180 * deg;
                    return (float)rad;
                }

                ImGui.AlignTextToFramePadding();
                ImGui.Text(label);
                ImGui.TableNextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                var deg = new System.Numerics.Vector3(RadToDeg(rad.X), RadToDeg(rad.Y), RadToDeg(rad.Z));

                if (ImGui.DragFloat3($"##{label}", ref deg, speed))
                {
                    rad.X = DegToRad(deg.X);
                    rad.Y = DegToRad(deg.Y);
                    rad.Z = DegToRad(deg.Z);
                }

                ImGui.PopItemWidth();
            }

            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                if (ImGui.BeginTable("Trans", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Scale");
                        ImGui.TableNextColumn();

                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                        ImGui.DragFloat3("##Scale", ref actor.mScale, 0.25f, 0, float.MaxValue);
                        ImGui.PopItemWidth();

                    ImGui.TableNextColumn();

                        EditFloat3RadAsDeg("Rotation", ref actor.mRotation, 0.25f);

                    ImGui.TableNextColumn();

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Translation");
                        ImGui.TableNextColumn();

                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                        ImGui.DragFloat3("##Translation", ref actor.mTranslation, 0.25f);
                        ImGui.PopItemWidth();

                    ImGui.EndTable();
                }
                ImGui.Unindent();
            }
        }

        private void DynamicParamNode(CourseActor actor)
        {
            if (ImGui.CollapsingHeader("Dynamic", ImGuiTreeNodeFlags.DefaultOpen))
            {
                List<string> actorParams = ParamDB.GetActorComponents(actor.mPackName);

                foreach (string param in actorParams)
                {
                    Dictionary<string, ParamDB.ComponentParam> dict = ParamDB.GetComponentParams(param);


                    if (dict.Keys.Count == 0)
                    {
                        continue;
                    }
                    ImGui.Indent();

                    ImGui.Text(param);
                    ImGui.Separator();

                    ImGui.Indent();

                    if (ImGui.BeginTable("DynamProps", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);

                        if (param == "ChildActorSelectName" && ChildActorParam.ActorHasChildParam(actor.mPackName))
                        {
                            string id = $"##{param}";
                            List<string> list = ChildActorParam.GetActorParams(actor.mPackName);
                            int selected = list.IndexOf(actor.mActorParameters["ChildActorSelectName"].ToString());
                            ImGui.Text("ChildParameters");
                            ImGui.TableNextColumn();
                            ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                            if (ImGui.Combo("##Parameters", ref selected, list.ToArray(), list.Count))
                            {
                                actor.mActorParameters["ChildActorSelectName"] = list[selected];
                            }
                            ImGui.PopItemWidth();
                        }
                        else
                        {
                            foreach (KeyValuePair<string, ParamDB.ComponentParam> pair in ParamDB.GetComponentParams(param))
                            {
                                string id = $"##{pair.Key}";

                                ImGui.AlignTextToFramePadding();
                                ImGui.Text(pair.Key);
                                ImGui.TableNextColumn();

                                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                                if (actor.mActorParameters.ContainsKey(pair.Key))
                                {
                                    var actorParam = actor.mActorParameters[pair.Key];

                                    if(pair.Value.IsSignedInt(out int minValue, out int maxValue))
                                    {
                                        int val_int = (int)actorParam;
                                        if (ImGui.InputInt(id, ref val_int))
                                        {
                                            actor.mActorParameters[pair.Key] = Math.Clamp(val_int, minValue, maxValue);
                                        }
                                    }
                                    else if (pair.Value.IsUnsignedInt(out minValue, out maxValue))
                                    {
                                        uint val_uint = (uint)actorParam;
                                        int val_int = unchecked((int)val_uint);
                                        if (ImGui.InputInt(id, ref val_int))
                                        {
                                            actor.mActorParameters[pair.Key] = unchecked((uint)Math.Clamp(val_int, minValue, maxValue));
                                        }
                                    }
                                    else if (pair.Value.IsBool())
                                    {
                                        bool val_bool = (bool)actorParam;
                                        if (ImGui.Checkbox(id, ref val_bool))
                                        {
                                            actor.mActorParameters[pair.Key] = val_bool;
                                        }

                                    }
                                    else if (pair.Value.IsFloat())
                                    {
                                        float val_float = (float)actorParam;
                                        if (ImGui.InputFloat(id, ref val_float))
                                        {
                                            actor.mActorParameters[pair.Key] = val_float;
                                        }
                                    }
                                    else if (pair.Value.IsString())
                                    {
                                        string val_string = (string)actorParam;
                                        if (ImGui.InputText(id, ref val_string, 1024))
                                        {
                                            actor.mActorParameters[pair.Key] = val_string;
                                        }
                                    }
                                    else if (pair.Value.IsDouble())
                                    {
                                        double val = (double)actorParam;
                                        if (ImGui.InputDouble(id, ref val))
                                        {
                                            actor.mActorParameters[pair.Key] = val;
                                        }
                                    }
                                }

                                ImGui.PopItemWidth();
                                ImGui.TableNextColumn();
                            }
                        }

                        ImGui.EndTable();
                    }
                    ImGui.Unindent();
                    ImGui.Unindent();
                }
            }
        }

        public Course GetCourse()
        {
            return course;
        }

        private async Task<(CourseActor?, KeyboardModifier modifiers)> PickLinkDestInViewportFor(CourseActor source)
        {
            var (picked, modifier) = await activeViewport.PickObject(
                            "Select the destination actor you wish to link to. -- Hold SHIFT to link multiple",
                            x => x is CourseActor && x != source);
            return (picked as CourseActor, modifier);
        }

        private async Task DeleteObjectsWithWarningPrompt(IReadOnlyList<object> objectsToDelete,
            CourseAreaEditContext ctx, string actionName)
        {
            var actors = objectsToDelete.OfType<CourseActor>();
            if (actors.Count() == 1)
                actionName = "Delete "+actors.ElementAt(0).mPackName;
            List<string> dstMsgStrs = [];
            List<string> srcMsgStrs = [];

            foreach (var actor in actors)
            {
                if (selectedArea.mLinkHolder.HasLinksWithDest(actor.mHash))
                {
                    var links = selectedArea.mLinkHolder.GetSrcHashesFromDest(actor.mHash);

                    foreach (KeyValuePair<string, List<ulong>> kvp in links)
                    {
                        var hashes = kvp.Value;

                        foreach (var hash in hashes)
                        {
                            /* only delete actors that the hash exists for...this may be caused by a user already deleting the source actor */
                            if (selectedArea.mActorHolder.TryGetActor(hash, out _))
                            {
                                dstMsgStrs.Add($"{selectedArea.mActorHolder[hash].mPackName} [{selectedArea.mActorHolder[hash].mName}]\n");
                            }
                        }
                    }

                    var destHashes = selectedArea.mLinkHolder.GetDestHashesFromSrc(actor.mHash);

                    foreach (KeyValuePair<string, List<ulong>> kvp in destHashes)
                    {
                        var hashes = kvp.Value;

                        foreach (var hash in hashes)
                        {
                            if (selectedArea.mActorHolder.TryGetActor(hash, out _))
                            {
                                srcMsgStrs.Add($"{selectedArea.mActorHolder[hash].mPackName} [{selectedArea.mActorHolder[hash].mName}]\n");
                            }
                        }
                    }
                }
            }

            bool noWarnings = (dstMsgStrs.Count == 0 && srcMsgStrs.Count == 0);

            if (!noWarnings)
            {
                var result = await OperationWarningDialog.ShowDialog(mPopupModalHost,
                "Deletion warning",
                "The object(s) you are about to delete " +
                "are being used in other places",
                ("As link source for", srcMsgStrs),
                ("As link destination for", dstMsgStrs));

                if (result == OperationWarningDialog.DialogResult.Cancel)
                    return;
            }

            var batchAction = ctx.BeginBatchAction();

            foreach (var actor in actors)
            {
                ctx.DeleteActor(actor);
            }

            batchAction.Commit($"{IconUtil.ICON_TRASH} {actionName}");
        }

        //TODO making this undoable
        private async Task DeleteLayerWithWarningPrompt(string layer,
            CourseActorHolder actorArray, CourseAreaEditContext ctx)
        {
            var actors = actorArray.mActors.FindAll(x => x.mLayer == layer);
            bool noWarnings = !(actors.Count > 0);

            if (!noWarnings)
            {
                List<string> warningActors = [];
                foreach (var actor in actors)
                {
                    if (selectedArea.mActorHolder.TryGetActor(actor.mHash, out _))
                    {
                        warningActors.Add($"{selectedArea.mActorHolder[actor.mHash].mPackName} [{selectedArea.mActorHolder[actor.mHash].mName}]\n");
                    }
                }

                var result = await OperationWarningDialog.ShowDialog(mPopupModalHost,
                "Deletion warning",
                "Deleting " + layer +
                " will delete the following actors",
                ("Actors", warningActors));

                if (result == OperationWarningDialog.DialogResult.Cancel)
                    return;
            }
            else
            {
                var result = await OperationWarningDialog.ShowDialog(mPopupModalHost,
                "Deletion warning",
                "Are you sure you want to delete " +
                layer+"?");

                if (result == OperationWarningDialog.DialogResult.Cancel)
                    return;
            }

            var batchAction = ctx.BeginBatchAction();

            foreach (var actor in actors)
            {
                ctx.DeleteActor(actor);
            }
            ctx.CommitAction(new PropertyFieldsSetUndo(
                    this, 
                    [("mLayersVisibility", new Dictionary<string, bool>(mLayersVisibility))],
                    $"{IconUtil.ICON_TRASH} Delete {layer}"
                )
            );
            mLayersVisibility.Remove(layer);

            batchAction.Commit($"{IconUtil.ICON_TRASH} Delete Layer: {layer}");
        }

        private async Task AddActorsWithSelectActorAndLayerWindow()
        {
            var viewport = activeViewport;
            var area = selectedArea;
            var ctx = areaScenes[selectedArea].EditContext;

            if(mOpenToolWindows.Any(x=>x is SelectActorAndLayerWindow))
                return;

            var window = new SelectActorAndLayerWindow(mLayersVisibility);
            mOpenToolWindows.Add(window);

            var result = await window.Result();
            if (!result.TryGetValue(out var resultVal))
                return;

            var (actorPack, layer) = resultVal;

            Vector3? pos;
            KeyboardModifier modifier;
            do
            {
                ImGui.SetWindowFocus(area.mAreaName);
                (pos, modifier) = await viewport.PickPosition(
                    $"Placing actor {actorPack} -- Hold SHIFT to place multiple", layer);
                if (!pos.TryGetValue(out var posVec))
                    return;

                var actor = new CourseActor(actorPack, area.mRootHash, layer);

                posVec.X = MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2;
                posVec.Y = MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2;
                posVec.Z = 0.0f;
                actor.mTranslation = posVec;
                var i = 0;
                do
                {
                    i++;
                } while (area.GetActors().Any(x => x.mName == $"{actor.mPackName}{i}"));
                actor.mName = $"{actor.mPackName}{i}";

                ctx.AddActor(actor);

            } while ((modifier & KeyboardModifier.Shift) > 0);
        }

        private async Task AddLayerWithLayerWindow()
        {
            var viewport = activeViewport;
            var area = selectedArea;
            var ctx = areaScenes[selectedArea].EditContext;

            if(mOpenToolWindows.Any(x=>x is SelectActorAndLayerWindow))
                return;

            var window = new SelectActorAndLayerWindow(mLayersVisibility, false);
            mOpenToolWindows.Add(window);

            var result = await window.Result();
            if (!result.TryGetValue(out var resultVal))
                return;

            var layer = result.Value.layer;

            if(layer == "PlayArea" || layer == "DecoArea")
            {
                int startIdx = layer == "DecoArea" ? 0:1;
                for (int i = startIdx; /*no condition*/; i++)  
                {  
                    if (!mLayersVisibility.ContainsKey($"{layer}{i}"))
                    {
                        layer += i;
                        break;
                    }
                }  
            }
            ctx.CommitAction(new PropertyFieldsSetUndo(
                    this, 
                    [("mLayersVisibility", new Dictionary<string, bool>(mLayersVisibility))],
                    $"{IconUtil.ICON_LAYER_GROUP} Added Layer: {layer}"
                )
            );
            mLayersVisibility[layer] = true;
        }

        interface IToolWindow
        {
            void Draw(ref bool windowOpen);
        }

        class SelectActorAndLayerWindow(IReadOnlyDictionary<string, bool> mLayersVisibility, bool addActors = true) : IToolWindow
        {
            public void Draw(ref bool windowOpen)
            {
                bool status;
                if(addActors)
                {
                    if (mSelectedActor == null)
                    {
                        status = ImGui.Begin("Add Actor###SelectActorLayer", ref windowOpen);
                        SelectActorToAdd();
                    }
                    else if(mSelectedLayer == null)
                    {
                        status = ImGui.Begin("Select Layer###SelectActorLayer", ref windowOpen);
                        SelectActorToAddLayer();
                    }
                    else
                    {
                        mPromise.TrySetResult((addActors ? mSelectedActor:"", mSelectedLayer));
                        windowOpen = false;
                        return;
                    }
                }
                else
                {
                    if(mSelectedLayer == null)
                    {
                        status = ImGui.Begin("Select Layer###SelectActorLayer", ref windowOpen);
                        SelectLayerToAdd();
                    }
                    else
                    {
                        mPromise.TrySetResult(("", mSelectedLayer));
                        windowOpen = false;
                        return;
                    }
                }

                if (ImGui.IsKeyDown(ImGuiKey.Escape))
                {
                    windowOpen = false;
                }

                if (!windowOpen)
                {
                    mPromise.TrySetResult(null);
                }

                if (status)
                {
                    ImGui.End();
                }

            }

            private void SelectActorToAdd()
            {
                ImGui.InputText("Search", ref mAddActorSearchQuery, 256);

                var filteredActors = ActorDataDB.GetTranslations().ToImmutableList();

                if (mAddActorSearchQuery != "")
                {
                    filteredActors = FuzzySharp.Process.ExtractAll(mAddActorSearchQuery, ActorDataDB.GetTranslations(), cutoff: 65)
                        .OrderByDescending(result => result.Score)
                        .Select(result => result.Value)
                        .ToImmutableList();
                }

                if (ImGui.BeginListBox("Select the actor you want to add.", ImGui.GetContentRegionAvail()))
                {
                    foreach (string actor in filteredActors)
                    {
                        ImGui.Selectable(actor);

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                            mSelectedActor = ActorDataDB.ReverseTranslate(actor);
                    }

                    ImGui.EndListBox();
                }
            }

            private void SelectActorToAddLayer()
            {
                ImGui.InputText("Search", ref mAddLayerSearchQuery, 256);

                var fileteredLayers = mLayersVisibility.Keys.ToArray().ToImmutableList();
                
                if (mAddLayerSearchQuery != "")
                {
                    fileteredLayers = FuzzySharp.Process.ExtractAll(mAddLayerSearchQuery, mLayersVisibility.Keys.ToArray(), cutoff: 65)
                        .OrderByDescending(result => result.Score)
                        .Select(result => result.Value)
                        .ToImmutableList();
                }

                if (ImGui.BeginListBox("Select the layer you want to add the actor to.", ImGui.GetContentRegionAvail()))
                {
                    foreach (string layer in fileteredLayers)
                    {
                        ImGui.Selectable(layer);

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                            mSelectedLayer = layer;
                    }

                    ImGui.EndListBox();
                }
            }

            // TODO, maybe find a way to combine with SelectActorToAddLayer(), if that's needed
            private void SelectLayerToAdd()
            {
                const int MaxLayerCount = 10;
                int layerCount = 0;

                ImGui.InputText("Search", ref mAddLayerSearchQuery, 256);

                string[] Layers = layerTypes
                    .Except(mLayersVisibility.Keys)
                    .ToArray();
                var fileteredLayers = Layers.ToImmutableList();
                
                if (mAddLayerSearchQuery != "")
                {
                    fileteredLayers = FuzzySharp.Process.ExtractAll(mAddLayerSearchQuery, Layers, cutoff: 65)
                        .OrderByDescending(result => result.Score)
                        .Select(result => result.Value)
                        .ToImmutableList();
                }

                if (ImGui.BeginListBox("Select the layer you want to add the actor to.", ImGui.GetContentRegionAvail()))
                {
                    for (var i = 0; i < fileteredLayers.Count; i++)
                    {
                        var layer = fileteredLayers[i];
                        layerCount = mLayersVisibility.Keys
                            .Count(x => x.StartsWith(layer) && NumberRegex.IsMatch(x.AsSpan(layer.Length..)));
                        if (layer == "PlayArea" || layer == "DecoArea")
                            layer += $" ({layerCount}/{MaxLayerCount})";

                        ImGui.BeginDisabled(layerCount == MaxLayerCount);

                        ImGui.Selectable(layer);

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        {
                            mSelectedLayer = fileteredLayers[i];
                        }

                        ImGui.EndDisabled();
                    }

                    ImGui.EndListBox();
                }
            }

            public Task<(string actor, string layer)?> Result() => mPromise.Task;

            private string? mSelectedActor;
            private string? mSelectedLayer;
            private TaskCompletionSource<(string actor, string layer)?> mPromise = new();
            private string mAddActorSearchQuery = "";
            private string mAddLayerSearchQuery = "";
        }

        class SaveFailureAlert : OkDialog<SaveFailureAlert>
        {
            protected override string Title => "Saving failed";

            protected override void DrawBody()
            {
                ImGui.Text("The course files may be open in an external app, or Super Mario Bros. Wonder may currently be running in an emulator. \n" +
                    "Close the emulator or external app and try again.");
            }
        }
    }
}
