using Fushigi.course;
using Fushigi.gl;
using Fushigi.gl.Bfres;
using Fushigi.param;
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

        (object? courseObj, PropertyFieldsCapture placementPropCapture, PropertyDictCapture dynamicPropCapture)
           propertyCapture = (null, 
            PropertyFieldsCapture.Empty, 
            PropertyDictCapture.Empty);

        readonly Course course;
        CourseArea selectedArea;

        readonly Dictionary<string, bool> mLayersVisibility = [];
        bool mHasFilledLayers = false;
        bool mAllLayersVisible = true;
        bool mShowAddActor = false;
        bool mShowSelectActorLayer = false;

        string mActorSearchText = "";

        CourseLink? mSelectedGlobalLink = null;
        string mAddActorSearchQuery = "";
        string mAddLayerSearchQuery = "";

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

        public CourseScene(Course course, GL gl)
        {
            this.course = course;
            selectedArea = course.GetArea(0);
            undoWindow = new UndoWindow();

            foreach (var area in course.GetAreas())
            {
                var areaScene = new CourseAreaScene(area, new CourseAreaSceneRoot(area));
                areaScenes[area] = areaScene;
                viewports[area] = new LevelViewport(area, gl, areaScene);
                lastSavedAction[area] = null;
            }

            activeViewport = viewports[selectedArea];
            PrepareResourcesLoad(gl);
        }

        public void PrepareResourcesLoad(GL gl)
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
            foreach (var file in resourceFiles)
                BfresCache.Load(gl, file);
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

        public void Save()
        {
            try
            {
                course.Save();
                foreach (var area in course.GetAreas())
                {
                    lastSavedAction[area] = areaScenes[area].EditContext.GetLastAction();
                }
            }
            catch (Exception ex)
            {
                MessageBox box = new MessageBox(MessageBox.MessageBoxType.Ok);
                box.Show("Error", ex.Message);
            }
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

            if (mShowAddActor)
            {
                SelectActorToAdd();
            }

            if (mShowSelectActorLayer)
            {
                SelectActorToAddLayer();
            }

            if (activeViewport.mEditorState == LevelViewport.EditorState.DeleteActorLinkCheck)
            {
                LinkDeletionCheck();
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
                    if (ImGui.IsWindowFocused())
                    {
                        selectedArea = area;
                        activeViewport = viewport;
                        mHasFilledLayers = false;
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
                    var pos = ImGui.GetCursorScreenPos();
                    ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
                    if (ImGui.BeginPopup($"AreaParams", ImGuiWindowFlags.NoMove))
                    {
                        AreaParameters(area.mAreaParams);
                        ImGui.EndPopup();
                    }
                }
            }

            if (lastCreatedViewports != viewports)
            {
                for (int i = 0; i < course.GetAreaCount(); i++)
                {
                    var area = course.GetArea(i);
                    if (area.mActorHolder.mActors.Any(x => x.mPackName == "PlayerLocator"))
                    {
                        ImGui.SetWindowFocus(area.GetName());
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

        private void SelectActorToAdd()
        {
            bool button = true;
            bool status = ImGui.Begin("Add Actor", ref button);

            ImGui.InputText("Search", ref mAddActorSearchQuery, 256);

            var filteredActors = ParamDB.GetActors().ToImmutableList();

            if (mAddActorSearchQuery != "")
            {
                filteredActors = FuzzySharp.Process.ExtractAll(mAddActorSearchQuery, ParamDB.GetActors(), cutoff: 65)
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
                    {
                        Console.WriteLine("Switching state to EditorState.SelectingActorLayer");
                        activeViewport.mEditorState = LevelViewport.EditorState.SelectingActorLayer;
                        activeViewport.mActorToAdd = actor;
                        mShowAddActor = false;
                        mShowSelectActorLayer = true;
                    }
                }

                ImGui.EndListBox();
            }

            if (ImGui.IsKeyDown(ImGuiKey.Escape))
            {
                button = false;
            }

            if (!button)
            {
                Console.WriteLine("Switching state to EditorState.Selecting");
                activeViewport.mEditorState = LevelViewport.EditorState.Selecting;
                mShowAddActor = false;
            }

            if (status)
            {
                ImGui.End();
            }
        }

        private void SelectActorToAddLayer()
        {
            bool button = true;
            bool status = ImGui.Begin("Select Layer", ref button);

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
                    {
                        Console.WriteLine("Switching state to EditorState.AddingActor");
                        activeViewport.mEditorState = LevelViewport.EditorState.AddingActor;
                        activeViewport.mLayerToAdd = layer;
                        mShowSelectActorLayer = false;
                    }
                }

                ImGui.EndListBox();
            }

            if (ImGui.IsKeyDown(ImGuiKey.Escape))
            {
                button = false;
            }

            if (!button)
            {
                Console.WriteLine("Switching state to EditorState.Selecting");
                activeViewport.mEditorState = LevelViewport.EditorState.Selecting;
                mShowAddActor = false;
            }

            if (status)
            {
                ImGui.End();
            }
        }

        private void LinkDeletionCheck()
        {
            var editContext = areaScenes[selectedArea].EditContext;

            var actors = editContext.GetSelectedObjects<CourseActor>();
            List<string> dstMsgStrs = new();
            List<string> srcMsgStr = new();

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
                                srcMsgStr.Add($"{selectedArea.mActorHolder[hash].mPackName} [{selectedArea.mActorHolder[hash].mName}]\n");
                            }
                        }
                    }
                }
            }

            /* nothing to worry about here */
            if (dstMsgStrs.Count == 0 && srcMsgStr.Count == 0)
            {
                var ctx = areaScenes[selectedArea].EditContext;

                if (ctx.IsAnySelected<CourseActor>())
                {
                    ctx.DeleteSelectedActors();
                }
                Console.WriteLine("Switching state to EditorState.Selecting");
                activeViewport.mEditorState = LevelViewport.EditorState.Selecting;
                return;
            }

            bool status = ImGui.Begin("Link Warning");

            if (srcMsgStr.Count > 0)
            {
                ImGui.Text("The actor you are about to delete is a source link for the following actors:");

                foreach (string s in srcMsgStr)
                {
                    ImGui.Text(s);
                }
            }

            if (dstMsgStrs.Count > 0)
            {
                ImGui.Text("The actor you are about to delete is a destination link for the following actors:");

                foreach (string s in dstMsgStrs)
                {
                    ImGui.Text(s);
                }
            }

            ImGui.Text(" Do you wish to continue?");

            if (ImGui.Button("Yes"))
            {
                Console.WriteLine("Switching state to EditorState.Selecting");
                editContext.DeleteSelectedActors();
                activeViewport.mEditorState = LevelViewport.EditorState.Selecting;
            }

            ImGui.SameLine();

            if (ImGui.Button("No"))
            {
                Console.WriteLine("Switching state to EditorState.Selecting");
                activeViewport.mEditorState = LevelViewport.EditorState.Selecting;
            }

            if (status)
            {
                ImGui.End();
            }
        }

        private void ActorsPanel()
        {
            ImGui.Begin("Actors");

            if (ImGui.Button("Add Actor"))
            {
                mShowAddActor = true;
            }

            ImGui.SameLine();

            if (ImGui.Button("Delete Actor"))
            {
                activeViewport.mEditorState = LevelViewport.EditorState.DeleteActorLinkCheck;
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

            ImGui.Checkbox("Wonder View", ref activeViewport.IsWonderView);

            ImGui.Separator();

            AreaLocalLinksView(selectedArea);
            
            ImGui.End();
        }
        
        private void RailLinksPanel()
        {
            ImGui.Begin("Actor to Rail Links");

            ImGui.Columns(4);
            ImGui.Text("Actor-Hash");
            ImGui.NextColumn();
            ImGui.Text("Rail");
            ImGui.NextColumn();
            ImGui.Text("Point");
            ImGui.NextColumn();
            ImGui.NextColumn();

            var ctx = areaScenes[selectedArea].EditContext;
            var rails = selectedArea.mRailHolder.mRails;
            var actors = selectedArea.mActorHolder.mActors;
            var railLinks = selectedArea.mRailLinksHolder.mLinks;

            for (int i = 0; i < railLinks.Count; i++)
            {
                ImGui.PushID(i);
                CourseActorToRailLink link = railLinks[i];

                string hash = link.mSourceActor.ToString();
                int actorIndex = actors.FindIndex(x => x.mHash == link.mSourceActor);
                if (ImGui.InputText("##actor", ref hash, 100) &&
                    ulong.TryParse(hash, out ulong hashInt))
                    link.mSourceActor = hashInt;
                if(actorIndex == -1)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled("Invalid");
                }

                ImGui.NextColumn();
                int railIndex = rails.FindIndex(x => x.mHash == link.mDestRail);
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
                ImGui.NextColumn();
                if (railIndex >= 0)
                {
                    int pointIndex = rails[railIndex].mPoints.FindIndex(x => x.mHash == link.mDestPoint);

                    if (pointIndex == -1)
                        pointIndex = 0;

                    if (ImGui.InputInt("##railpoint", ref pointIndex))
                        pointIndex = Math.Clamp(pointIndex, 0, rails[railIndex].mPoints.Count - 1);

                    link.mDestPoint = rails[railIndex].mPoints[pointIndex].mHash;
                }

                ImGui.NextColumn();
                if (ImGui.Button("Delete", new Vector2(ImGui.GetContentRegionAvail().X * 0.65f, 0)))
                {
                    ctx.DeleteRailLink(link);
                    i--;
                }


                ImGui.NextColumn();
                ImGui.PopID();
            }

            float width = ImGui.GetItemRectMax().X - ImGui.GetCursorScreenPos().X;

            ImGui.Columns(1);
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
                if((propertyCapture.placementPropCapture.HasChangesSinceLastCheckpoint() ||
                    propertyCapture.dynamicPropCapture.HasChangesSinceLastCheckpoint()))
                {
                    propertyCapture = (null, PropertyFieldsCapture.Empty, PropertyDictCapture.Empty);
                }

                #region Actor UI
                string actorName = mSelectedActor.mPackName;
                string name = mSelectedActor.mName;

                ImGui.Columns(2);
                ImGui.AlignTextToFramePadding();
                string packName = mSelectedActor.mPackName;

                ImGui.Text("Actor Name");
                ImGui.NextColumn();
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
                ImGui.NextColumn();

                ImGui.Text("Actor Hash");
                ImGui.NextColumn();
                string hash = mSelectedActor.mHash.ToString();
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                ImGui.InputText("##Actor Hash", ref hash, 256, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopItemWidth();
                ImGui.NextColumn();

                ImGui.Separator();

                ImGui.Columns(2);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Name");

                ImGui.NextColumn();
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                if (ImGui.InputText($"##{name}", ref name, 512, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    mSelectedActor.mName = name;
                }

                ImGui.PopItemWidth();
                ImGui.NextColumn();

                ImGui.Text("Layer");

                ImGui.NextColumn();

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

                ImGui.Columns(1);

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
                            activeViewport.mNewLinkType = linkType;
                            activeViewport.mIsLinkNew = true;
                            activeViewport.mEditorState = LevelViewport.EditorState.SelectingLinkDest;
                            ImGui.SetWindowFocus(selectedArea.GetName());
                        }
                    }

                    ImGui.EndCombo();
                }

                var destHashes = selectedArea.mLinkHolder.GetDestHashesFromSrc(mSelectedActor.mHash);

                foreach ((string linkName, List<ulong> hashArray) in destHashes)
                {
                    ImGui.Text(linkName);

                    ImGui.Columns(3);

                    for (int i = 0; i < hashArray.Count; i++)
                    {
                        ImGui.PushID($"{hashArray[i].ToString()}_{i}");
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);
                        ImGui.Text("Destination");
                        ImGui.NextColumn();

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

                        ImGui.NextColumn();

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
                            activeViewport.mEditorState = LevelViewport.EditorState.SelectingLinkDest;
                            activeViewport.CurCourseLink = selectedArea.mLinkHolder.GetLinkWithDestHash(hashArray[i]);
                            ImGui.SetWindowFocus(selectedArea.GetName());
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

                        ImGui.NextColumn();

                        ImGui.PopID();
                    }

                    ImGui.Separator();

                }
                #endregion

                bool needsRecapture = false;

                if (!ImGui.IsAnyItemActive())
                {
                    if (propertyCapture.placementPropCapture.TryGetRevertable(out var revertable, out var names))
                    {
                        editContext.CommitAction(revertable);
                        needsRecapture = true;
                    }

                    if (propertyCapture.dynamicPropCapture.TryGetRevertable(out revertable, out names))
                    {
                        editContext.CommitAction(revertable);
                        needsRecapture = true;
                    }
                }
                if(needsRecapture || propertyCapture.courseObj != mSelectedActor)
                {
                    Console.WriteLine("Capturing");
                    propertyCapture = (
                        mSelectedActor,
                        new PropertyFieldsCapture(mSelectedActor),
                        new PropertyDictCapture(mSelectedActor.mActorParameters)
                    );
                }

                propertyCapture.placementPropCapture.MakeCheckpoint();
                propertyCapture.dynamicPropCapture.MakeCheckpoint();
            }
            else if (editContext.IsSingleObjectSelected(out CourseUnit? mSelectedUnit))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Selected BG Unit");

                ImGui.Separator();

                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);
                    ImGui.Text("Model Type"); ImGui.NextColumn();

                    ImGui.Combo("##mModelType", ref Unsafe.As<CourseUnit.ModelType, int>(ref mSelectedUnit.mModelType),
                        CourseUnit.ModelTypeNames, CourseUnit.ModelTypeNames.Length);
                    ImGui.NextColumn();

                    ImGui.Text("Skin Division"); ImGui.NextColumn();
                    ImGui.Combo("##SkinDivision", ref Unsafe.As<CourseUnit.SkinDivision, int>(ref mSelectedUnit.mSkinDivision),
                        CourseUnit.SkinDivisionNames, CourseUnit.SkinDivisionNames.Length);

                    ImGui.Columns(1);
                }
            }
            else if (editContext.IsSingleObjectSelected(out BGUnitRail? mSelectedUnitRail))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Selected BG Unit Rail");

                ImGui.Separator();

                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);
                    ImGui.Text("IsClosed"); ImGui.NextColumn();
                    if (ImGui.Checkbox("##IsClosed", ref mSelectedUnitRail.IsClosed))
                        mSelectedUnitRail.mCourseUnit.GenerateTileSubUnits();

                    ImGui.NextColumn();

                    //Depth editing for bg unit. All points share the same depth, so batch edit the Z point
                    float depth = mSelectedUnitRail.Points.Count == 0 ? 0 : mSelectedUnitRail.Points[0].Position.Z;

                    ImGui.Text("Z Depth"); ImGui.NextColumn();
                    if (ImGui.DragFloat("##Depth", ref depth, 0.1f))
                    {
                        //Update depth to all points
                        foreach (var p in mSelectedUnitRail.Points)
                            p.Position = new System.Numerics.Vector3(p.Position.X, p.Position.Y, depth);
                        mSelectedUnitRail.mCourseUnit.GenerateTileSubUnits();
                    }
                    ImGui.NextColumn();

                    ImGui.Columns(1);
                }
            }
            else if (mSelectedGlobalLink != null)
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
                    ImGui.Columns(2);
                    ImGui.Text("Source Hash"); ImGui.NextColumn();
                    string srcHash = mSelectedGlobalLink.mSource.ToString();
                    if (ImGui.InputText("##Source Hash", ref srcHash, 256, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        mSelectedGlobalLink.mSource = Convert.ToUInt64(srcHash);
                    }

                    ImGui.NextColumn();

                    ImGui.Text("Destination Hash"); ImGui.NextColumn();
                    string destHash = mSelectedGlobalLink.mDest.ToString();
                    if (ImGui.InputText("##Dest Hash", ref destHash, 256, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        mSelectedGlobalLink.mDest = Convert.ToUInt64(destHash);
                    }

                    ImGui.NextColumn();

                    ImGui.Text("Link Type"); ImGui.NextColumn();

                    List<string> types = linkTypes.ToList();
                    int idx = types.IndexOf(mSelectedGlobalLink.mLinkName);
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.Combo("##Link Type", ref idx, linkTypes, linkTypes.Length))
                    {
                        mSelectedGlobalLink.mLinkName = linkTypes[idx];
                    }

                    ImGui.Columns(1);
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
                    ImGui.Columns(2);
                    ImGui.Text("Hash"); ImGui.NextColumn();
                    string hash = mSelectedRail.mHash.ToString();
                    if (ImGui.InputText("##Hash", ref hash, 256, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        mSelectedRail.mHash = Convert.ToUInt64(hash);
                    }

                    ImGui.NextColumn();
                    ImGui.Text("IsClosed");
                    ImGui.NextColumn();
                    ImGui.Checkbox("##IsClosed", ref mSelectedRail.mIsClosed);

                    ImGui.Columns(1);
                }

                if (ImGui.CollapsingHeader("Dynamic Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);

                    foreach (KeyValuePair<string, object> param in mSelectedRail.mParameters)
                    {
                        string type = param.Value.GetType().ToString();
                        ImGui.Text(param.Key);
                        ImGui.NextColumn();

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

                        ImGui.NextColumn();
                    }
                }
            }
            else if (editContext.IsSingleObjectSelected(out CourseRail.CourseRailPoint? mSelectedRailPoint))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Selected Rail Point");
                ImGui.NewLine();
                ImGui.Separator();

                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);
                    ImGui.Text("Hash"); ImGui.NextColumn();
                    string hash = mSelectedRailPoint.mHash.ToString();
                    if (ImGui.InputText("##Hash", ref hash, 256, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        mSelectedRailPoint.mHash = Convert.ToUInt64(hash);
                    }
                    ImGui.NextColumn();

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Translation");
                    ImGui.NextColumn();

                    ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                    ImGui.DragFloat3("##Translation", ref mSelectedRailPoint.mTranslate, 0.25f);
                    ImGui.PopItemWidth();

                    ImGui.Columns(1);
                }

                if (ImGui.CollapsingHeader("Dynamic Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);

                    foreach (KeyValuePair<string, object> param in mSelectedRailPoint.mParameters)
                    {
                        string type = param.Value.GetType().ToString();
                        ImGui.Text(param.Key);
                        ImGui.NextColumn();

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

                        ImGui.NextColumn();
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

        private static void AreaParameters(CourseArea.AreaParam area)
        {
            ParamHolder areaParams = ParamLoader.GetHolder("AreaParam");
            var pos = ImGui.GetCursorScreenPos();
            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(400, 800), ImGuiCond.Once);

            if (ImGui.Begin("Area Parameters", ImGuiWindowFlags.NoMove))
            {
                ImGui.Columns(2);

                foreach (string key in areaParams.Keys)
                {
                    string paramType = areaParams[key];

                    //if (!area.ContainsParam(key))
                    //{
                    //    continue;
                    //}

                    ImGui.Text(key);
                    ImGui.NextColumn();

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

                    ImGui.NextColumn();
                }
                ImGui.End();
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
                    editContext.DeselectAllOfType<CourseUnit>();
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

                        ImGui.Columns(2);

                        void SelectRail()
                        {
                            editContext.DeselectAllOfType<BGUnitRail>();
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

                        ImGui.NextColumn();

                        ImGui.TextDisabled($"(Num Points: {rail.Points.Count})");

                        ImGui.Columns(1);

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

                    foreach (var wall in unit.Walls)
                    {
                        if (wall.InternalRails.Count > 0)
                        {
                            bool ex = ImGui.TreeNodeEx($"##{name}Wall{unit.Walls.IndexOf(wall)}", ImGuiTreeNodeFlags.DefaultOpen);
                            ImGui.SameLine();

                            RailListItem("Wall", wall.ExternalRail, unit.Walls.IndexOf(wall));

                            ImGui.Indent();

                            if (ex)
                            {
                                foreach (var rail in wall.InternalRails)
                                    RailListItem("Internal Rail", rail, wall.InternalRails.IndexOf(rail));
                            }
                            ImGui.Unindent();

                            ImGui.TreePop();
                        }
                        else
                        {
                            RailListItem("Wall", wall.ExternalRail, unit.Walls.IndexOf(wall));
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
                var rail_node_flags = ImGuiTreeNodeFlags.None;
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
            for (int i = 0; i < linkHolder.mLinks.Count; i++)
            {
                CourseLink link = linkHolder.mLinks[i];
                if (ImGui.Selectable($"Link {i}"))
                {
                    mSelectedGlobalLink = link;
                }
            }
        }
        
        //VERY ROUGH BASE
        //Still need to implement recursion on getting links, currently just displays the top most links
        private void AreaLocalLinksView(CourseArea area)
        {
            var links = area.mLinkHolder;
            var editContext = areaScenes[selectedArea].EditContext;

            float em = ImGui.GetFrameHeight();
            var wcMin = ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetScrollY());
            var wcMax = wcMin + ImGui.GetContentRegionAvail();

            RecursiveLinkFind(area, links, editContext, em);

            ImGui.PopClipRect();

            ImGui.EndChild();
        }

        private void RecursiveLinkFind(CourseArea area, CourseLinkHolder links, CourseAreaEditContext editContext, float em)
        {
            foreach (CourseActor actor in area.GetActors().Where(x => !links.GetSrcHashesFromDest(x.mActorHash).Any() && links.GetDestHashesFromSrc(x.mActorHash).Any()))
            {
                ImGuiTreeNodeFlags node_flags = ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.OpenOnArrow;
                ImGui.PushID(actor.mActorHash.ToString());
                bool expanded = false;
                bool isVisible = true;
                float margin = 1.5f * em;
                float headerHeight = 1.4f * em;
                Vector2 cp = ImGui.GetCursorScreenPos();
                expanded = ImGui.TreeNodeEx(actor.mActorHash.ToString(), node_flags, actor.mActorName);

                if (ImGui.IsItemFocused())
                {
                    activeViewport.SelectedActor(actor);
                }

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                {
                    activeViewport.FrameSelectedActor(actor);
                }

                if (!isVisible)
                    ImGui.BeginDisabled();

                if (expanded)
                {
                    foreach (var link in links.GetDestHashesFromSrc(actor.mActorHash))
                    {
                        if(ImGui.TreeNodeEx(actor.mActorHash.ToString() + link.Key, ImGuiTreeNodeFlags.FramePadding, link.Key))
                        {
                            foreach (CourseActor linkActor in area.GetActors().Where(x => link.Value.Contains(x.mActorHash)))
                            {
                                var act = linkActor;
                                string actorName = act.mActorName;
                                string name = act.mName;
                                ulong actorHash = act.mActorHash;
                                string actorLink = link.Key;
                                //Check if the node is within the necessary search filter requirements if search is used
                                bool HasText = act.mName.IndexOf(mActorSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            act.mActorName.IndexOf(mActorSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            act.ToString().Equals(mActorSearchText);

                                if (!HasText)
                                    continue;

                                bool isSelected = editContext.IsSelected(act);

                                ImGui.PushID(actorHash.ToString());
                                ImGui.Columns(2);
                                
                                if (ImGui.Selectable(actorName, isSelected, ImGuiSelectableFlags.SpanAllColumns))
                                {
                                    activeViewport.SelectedActor(act);
                                }
                                else if (ImGui.IsItemFocused())
                                {
                                    activeViewport.SelectedActor(act);
                                }

                                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                                {
                                    activeViewport.FrameSelectedActor(act);
                                }


                                ImGui.NextColumn();
                                ImGui.BeginDisabled();
                                ImGui.Text(name);
                                ImGui.EndDisabled();
                                ImGui.Columns(1);

                                ImGui.PopID();
                                ImGui.TreePop();
                            }
                        }
                    }
                    ImGui.TreePop();
                }
            
                if (!isVisible)
                    ImGui.EndDisabled();

                ImGui.PopID();
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

            ImGui.Spacing();
            foreach (string layer in mLayersVisibility.Keys)
            {
                ImGui.PushID(layer);
                cp = ImGui.GetCursorScreenPos();
                bool expanded = false;
                bool isVisible = true;

                if (!isSearch)
                {
                    expanded = ImGui.TreeNodeEx("TreeNode", ImGuiTreeNodeFlags.FramePadding, layer);

                    ImGui.PushClipRect(wcMin, wcMax, false);
                    ImGui.SetCursorScreenPos(new Vector2(wcMax.X - (margin + em) / 2, cp.Y));
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

                if (!isVisible)
                    ImGui.BeginDisabled();

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
                        ImGui.Columns(2);
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


                        ImGui.NextColumn();
                        ImGui.BeginDisabled();
                        ImGui.Text(name);
                        ImGui.EndDisabled();
                        ImGui.Columns(1);

                        ImGui.PopID();
                    }

                    if (!isSearch)
                        ImGui.TreePop();
                }

                if (!isVisible)
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

            var topLeft = ImGui.GetCursorScreenPos();

            ImGui.SetNextItemAllowOverlap();
            ImGui.SetCursorScreenPos(topLeft);

                    //ImGui.SetNextItemAllowOverlap();
            var size = ImGui.GetContentRegionAvail();

            ImGui.SetNextItemAllowOverlap();
            ImGui.SetCursorScreenPos(topLeft);

            var cam = view.Camera;
            var camSize = view.GetCameraSizeIn2DWorldSpace();

            Vector4 bounds = Vector4.Zero;
            foreach(var actor in area.GetActors())
            {
                bounds = new(Math.Min(bounds.X, actor.mTranslation.X),
                Math.Min(bounds.Y, actor.mTranslation.Y),
                Math.Max(bounds.Z, actor.mTranslation.X),
                Math.Max(bounds.W, actor.mTranslation.Y));
            }
            var levelRect = new Vector2(bounds.Z-bounds.X, bounds.W - bounds.Y);

            float tanFOV = MathF.Tan(cam.Fov / 2);

            var ratio = size.X/levelRect.X < size.Y/levelRect.Y ? size.X/levelRect.X : size.Y/levelRect.Y;
            var miniRect = levelRect*ratio;
            var miniCam = new Vector2(cam.Target.X, -cam.Target.Y)*ratio;
            var miniCamSize = camSize*ratio;
            var miniSaveCam = new Vector2(camSave.X, -camSave.Y)*ratio;
            var center = new Vector2((size.X - miniRect.X)/2, (size.Y - miniRect.Y)/2);

            var col = ImGuiCol.ButtonActive;

            //ImGui.SetNextItemAllowOverlap();
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsMouseDown(ImGuiMouseButton.Left) 
            && ImGui.IsWindowHovered())
            {
                camSave = cam.Target;
            }

            if ((ImGui.IsMouseDown(ImGuiMouseButton.Left) || ImGui.IsMouseDown(ImGuiMouseButton.Right))
            && ImGui.IsWindowFocused() &&
            ((!ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            || ImGui.IsWindowHovered()))
            {
                if (camSave != default)
                {
                    col = ImGuiCol.TextDisabled;
                    ImGui.GetWindowDrawList().AddRect(topLeft + miniSaveCam - miniCamSize/2 + new Vector2(0, miniRect.Y) + center, 
                    topLeft + miniSaveCam + miniCamSize/2 + new Vector2(0, miniRect.Y) + center, 
                    ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Button]),6,0,3);
                }

                var pos = ImGui.GetMousePos();
                cam.Target = new((pos.X - (topLeft.X + center.X))/ratio,
                (-pos.Y + topLeft.Y + center.Y + miniRect.Y)/ratio, cam.Target.Z);
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && !ImGui.IsMouseDown(ImGuiMouseButton.Left)
            && camSave != default)
            {
                cam.Target = camSave;
                camSave = default;
            }

            ImGui.GetWindowDrawList().AddRect(topLeft + center, 
            topLeft + miniRect + center, 
            ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]),6,0,3);

            ImGui.GetWindowDrawList().AddRect(topLeft + miniCam - miniCamSize/2 + new Vector2(0, miniRect.Y) + center, 
            topLeft + miniCam + miniCamSize/2 + new Vector2(0, miniRect.Y) + center, 
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
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                var deg = new System.Numerics.Vector3(RadToDeg(rad.X), RadToDeg(rad.Y), RadToDeg(rad.Z));

                if (ImGui.DragFloat3($"##{label}", ref deg, speed))
                {
                    rad.X = DegToRad(deg.X);
                    rad.Y = DegToRad(deg.Y);
                    rad.Z = DegToRad(deg.Z);
                }

                ImGui.PopItemWidth();

                ImGui.NextColumn();
            }

            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                ImGui.Columns(2);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Scale");
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                ImGui.DragFloat3("##Scale", ref actor.mScale, 0.25f, 0, float.MaxValue);
                ImGui.PopItemWidth();

                ImGui.NextColumn();

                ImGui.Columns(1);
                ImGui.Unindent();

                ImGui.Indent();
                ImGui.Columns(2);

                EditFloat3RadAsDeg("Rotation", ref actor.mRotation, 0.25f);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Translation");
                ImGui.NextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                ImGui.DragFloat3("##Translation", ref actor.mTranslation, 0.25f);
                ImGui.PopItemWidth();

                ImGui.Columns(1);
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

                    ImGui.Columns(2);

                    if (param == "ChildActorSelectName" && ChildActorParam.ActorHasChildParam(actor.mPackName))
                    {
                        string id = $"##{param}";
                        List<string> list = ChildActorParam.GetActorParams(actor.mPackName);
                        int selected = list.IndexOf(actor.mActorParameters["ChildActorSelectName"].ToString());
                        ImGui.Text("ChildParameters");
                        ImGui.NextColumn();
                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                        if (ImGui.Combo("##Parameters", ref selected, list.ToArray(), list.Count))
                        {
                            actor.mActorParameters["ChildActorSelectName"] = list[selected];
                        }
                    }
                    else
                    {
                        foreach (KeyValuePair<string, ParamDB.ComponentParam> pair in ParamDB.GetComponentParams(param))
                        {
                            string id = $"##{pair.Key}";

                            ImGui.AlignTextToFramePadding();
                            ImGui.Text(pair.Key);
                            ImGui.NextColumn();

                            ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                            if (actor.mActorParameters.ContainsKey(pair.Key))
                            {
                                var actorParam = actor.mActorParameters[pair.Key];

                                switch (pair.Value.Type)
                                {
                                    case "U8":
                                    case "S16":
                                    case "S32":
                                        int val_int = (int)actorParam;
                                        if (ImGui.InputInt(id, ref val_int))
                                        {
                                            actor.mActorParameters[pair.Key] = val_int;
                                        }
                                        break;
                                    case "Bool":
                                        bool val_bool = (bool)actorParam;
                                        if (ImGui.Checkbox(id, ref val_bool))
                                        {
                                            actor.mActorParameters[pair.Key] = val_bool;
                                        }
                                        break;
                                    case "F32":
                                        float val_float = (float)actorParam;
                                        if (ImGui.InputFloat(id, ref val_float))
                                        {
                                            actor.mActorParameters[pair.Key] = val_float;
                                        }
                                        break;
                                    case "String":
                                        string val_string = (string)actorParam;
                                        if (ImGui.InputText(id, ref val_string, 1024))
                                        {
                                            actor.mActorParameters[pair.Key] = val_string;
                                        }
                                        break;
                                    case "F64":
                                        double val = (double)actorParam;
                                        if (ImGui.InputDouble(id, ref val))
                                        {
                                            actor.mActorParameters[pair.Key] = val;
                                        }
                                        break;
                                }
                            }

                            ImGui.PopItemWidth();

                            ImGui.NextColumn();
                        }
                    }

                    ImGui.Columns(1);

                    ImGui.Unindent();
                    ImGui.Unindent();

                }
            }
        }

        public Course GetCourse()
        {
            return course;
        }
    }
}
