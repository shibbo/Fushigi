using Fushigi.Byml;
using Fushigi.course;
using Fushigi.param;
using Fushigi.rstb;
using Fushigi.util;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using Silk.NET.Windowing;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Fushigi.ui.widgets
{
    class CourseScene
    {
        Dictionary<CourseArea, LevelViewport> viewports = [];
        Dictionary<CourseArea, LevelViewport>? lastCreatedViewports;
        LevelViewport activeViewport;

        readonly Course course;
        CourseArea selectedArea;

        readonly Dictionary<string, bool> mLayersVisibility = [];
        bool mHasFilledLayers = false;
        bool mAllLayersVisible = true;
        bool mShowAddActor = false;

        CourseActor? mSelectedActor = null;
        CourseUnit? mSelectedUnit = null;
        BGUnitRail? mSelectedUnitRail = null;

        public CourseScene(Course course, GL gl)
        {
            this.course = course;
            selectedArea = course.GetArea(0);

            foreach (var area in course.GetAreas())
            {
                viewports[area] = new LevelViewport(area, gl, new CourseAreaEditContext(area));
            }

            activeViewport = viewports[selectedArea];
        }

        public void DeselectAll()
        {
            mSelectedActor = null;
            mSelectedUnit = null;
            mSelectedUnitRail = null;
        }

        public void DrawUI(GL gl)
        {
            ActorsPanel();

            SelectionParameterPanel();

            RailsPanel();

            BGUnitPanel();

            if (mShowAddActor)
            {
                SelectActor();
            }

            if (activeViewport.mEditorState == LevelViewport.EditorState.DeleteActorLinkCheck)
            {
                LinkDeletionCheck();
            }

            
            ulong selectionVersionBefore = activeViewport.mEditContext.SelectionVersion;

            bool status = ImGui.Begin("Viewports");

            ImGui.DockSpace(0x100, ImGui.GetContentRegionAvail());

            for (int i = 0; i < course.GetAreaCount(); i++)
            {
                var area = course.GetArea(i);
                var viewport = viewports[area];

                ImGui.SetNextWindowDockID(0x100, ImGuiCond.Once);

                if (ImGui.Begin(area.GetName()))
                {
                    if(ImGui.IsWindowFocused())
                    {
                        selectedArea = area;
                        activeViewport = viewport;
                    }

                    var topLeft = ImGui.GetCursorScreenPos();
                    var size = ImGui.GetContentRegionAvail();

                    viewport.Draw(ImGui.GetContentRegionAvail(), mLayersVisibility);
                    if(activeViewport != viewport)
                        ImGui.GetWindowDrawList().AddRectFilled(topLeft, topLeft + size, 0x44000000);

                    //Allow button press, align to top of the screen
                    ImGui.SetItemAllowOverlap();
                    ImGui.SetCursorScreenPos(topLeft);

                    //Load popup when button is pressed
                    if (ImGui.Button("Area Parameters"))
                        ImGui.OpenPopup("AreaParams");

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
                    if(area.mActorHolder.GetActors().Any(x=>x.mActorName=="PlayerLocator"))
                    {
                        ImGui.SetWindowFocus(area.GetName());
                        break;
                    }

                }

                lastCreatedViewports = viewports;
            }

            if (activeViewport.mEditContext.SelectionVersion != selectionVersionBefore)
            {
                DeselectAll();
                if (activeViewport.mEditContext.IsSingleObjectSelected<CourseActor>(out var actor))
                    mSelectedActor = actor;
                if (activeViewport.mEditContext.IsSingleObjectSelected<BGUnitRail>(out var bgUnitRail))
                {
                    mSelectedUnitRail = bgUnitRail;
                }
            }

            if (status)
                ImGui.End();
        }

        public void Save()
        {
            RSTB resource_table = new RSTB();
            resource_table.Load();

            //Save each course area to current romfs folder
            foreach (var area in this.course.GetAreas())
            {
                Console.WriteLine($"Saving area {area.GetName()}...");

                area.Save(resource_table);
            }

            resource_table.Save();
        }

        private void SelectActor()
        {
            bool button = true;
            bool status = ImGui.Begin("Add Actor", ref button);

            ImGui.BeginListBox("Select the actor you want to add.", ImGui.GetContentRegionAvail());

            if (mSelectedActor != null)
            {
                 ImGui.Selectable("Selected Actor");

                 if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                 {
                     Console.WriteLine("Switching state to EditorState.AddingActor");
                     activeViewport.mEditorState = LevelViewport.EditorState.AddingActor;
                     activeViewport.mActorToAdd = mSelectedActor.mActorName;
                     mShowAddActor = false;
                 }
            }
            
            foreach (string actor in ParamDB.GetActors())
            {
                ImGui.Selectable(actor);

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                {
                    Console.WriteLine("Switching state to EditorState.AddingActor");
                    activeViewport.mEditorState = LevelViewport.EditorState.AddingActor;
                    activeViewport.mActorToAdd = actor;
                    mShowAddActor = false;
                }
            }

            ImGui.EndListBox();

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
            var actors = activeViewport.mEditContext.GetSelectedObjects<CourseActor>();
            List<string> dstMsgStrs = new();
            List<string> srcMsgStr = new();

            foreach (var actor in actors)
            {
                if (activeViewport.mEditContext.IsActorDestForLink(actor))
                {
                    var links = selectedArea.mLinkHolder.GetSrcHashesFromDest(actor.GetHash());

                    foreach (KeyValuePair<string, List<ulong>> kvp in links)
                    {
                        var hashes = kvp.Value;

                        foreach (var hash in hashes)
                        {
                            /* only delete actors that the hash exists for...this may be caused by a user already deleting the source actor */
                            if (selectedArea.mActorHolder.HasHash(hash))
                            {
                                dstMsgStrs.Add($"{selectedArea.mActorHolder[hash].mActorName} [{selectedArea.mActorHolder[hash].mName}]\n");
                            }
                        }
                    }

                    var destHashes = selectedArea.mLinkHolder.GetDestHashesFromSrc(actor.GetHash());

                    foreach (KeyValuePair<string, List<ulong>> kvp in destHashes)
                    {
                        var hashes = kvp.Value;

                        foreach (var hash in hashes)
                        {
                            if (selectedArea.mActorHolder.HasHash(hash))
                            {
                                srcMsgStr.Add($"{selectedArea.mActorHolder[hash].mActorName} [{selectedArea.mActorHolder[hash].mName}]\n");
                            }
                        }
                    }
                }
            }

            /* nothing to worry about here */
            if (dstMsgStrs.Count == 0 && srcMsgStr.Count == 0)
            {
                Console.WriteLine("Switching state to EditorState.DeletingActor");
                activeViewport.mEditContext.DeleteSelectedActors();
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
                activeViewport.mEditContext.DeleteSelectedActors();
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

        private void SelectionParameterPanel()
        {
            bool status = ImGui.Begin("Selection Parameters", ImGuiWindowFlags.AlwaysVerticalScrollbar);

            if (mSelectedActor != null)
            {
                string actorName = mSelectedActor.mActorName;
                string name = mSelectedActor.mName;

                ImGui.AlignTextToFramePadding();
                string tempName = mSelectedActor.mActorName;
                if (ImGui.InputText("Actor Name", ref tempName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (ParamDB.GetActors().Contains(tempName))
                    {
                        activeViewport.mEditContext.SetActorName(mSelectedActor, tempName);
                        mSelectedActor.InitializeDefaultDynamicParams();
                    }
                }

                ImGui.Separator();

                ImGui.Columns(2);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Name");

                ImGui.NextColumn();
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                if (ImGui.InputText($"##{name}", ref name, 512, ImGuiInputTextFlags.EnterReturnsTrue)) {
                    activeViewport.mEditContext.SetObjectName(mSelectedActor, name);
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

                string[] linkTypes = [ 
                    "BasicSignal", "Create", "Delete", "CreateRelativePos", 
                    "CullingReference", "NextGoToParallel", "Bind", "NoticeDeath", 
                    "Contents", "PopUp", "ParamRefForChild", "Connection", "Follow",
                    "Reference",
                ];

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

                var destHashes = selectedArea.mLinkHolder.GetDestHashesFromSrc(mSelectedActor.GetHash());

                foreach ((string linkName, List<ulong> hashArray) in destHashes) {
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

                        uint WithAlphaFactor(uint color, float factor) => color & 0xFFFFFF | ((uint)((color >> 24)*factor) << 24);

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
                        ImGui.GetWindowDrawList().AddText(cursorSP + new Vector2((deleteButtonWidth - ImGui.CalcTextSize(deleteIcon).X)/2, padding.Y),
                            WithAlphaFactor(ImGui.GetColorU32(ImGuiCol.Text), ImGui.IsItemHovered() ? 1 : 0.5f), 
                            deleteIcon);

                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Delete Link");

                        if (clicked)
                            activeViewport.mEditContext.DeleteLink(linkName, mSelectedActor.mActorHash, hashArray[i]);

                        ImGui.NextColumn();

                        ImGui.PopID();
                    }

                    ImGui.Separator();
                    
                }

            }
            else if (mSelectedUnit != null)
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
            else if (mSelectedUnitRail != null)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Selected BG Unit Rail");

                ImGui.Separator();

                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);
                    ImGui.Text("IsClosed"); ImGui.NextColumn();
                    ImGui.Checkbox("##IsClosed", ref mSelectedUnitRail.IsClosed); ImGui.NextColumn();

                    ImGui.Text("IsInternal"); ImGui.NextColumn();
                    ImGui.Checkbox("##IsInternal", ref mSelectedUnitRail.IsInternal); ImGui.NextColumn();

                    ImGui.Columns(1);
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
            foreach (CourseActor actor in actorArray.GetActors())
            {
                string actorLayer = actor.mLayer;
                mLayersVisibility[actorLayer] = true;
            }

            mHasFilledLayers = true;
        }

        private void CourseUnitView(CourseUnitHolder unitHolder)
        {
            ImGui.Text("Select a Wall");
            ImGui.Text("Alt + Left Click to add point");

            if (ImGui.Button("Add Tile Unit", new Vector2(100, 22)))
            {
                unitHolder.mUnits.Add(new CourseUnit());
            }

            List<CourseUnit> removed_tile_units = new List<CourseUnit>();

            foreach (var unit in unitHolder.mUnits)
            {
                var tree_flags = ImGuiTreeNodeFlags.None;
                string name = $"Tile Unit {unitHolder.mUnits.IndexOf(unit)}";

                ImGui.AlignTextToFramePadding();
                bool expanded = ImGui.TreeNodeEx($"##{name}", ImGuiTreeNodeFlags.DefaultOpen);

                ImGui.SameLine();
                if (ImGui.Checkbox($"##Visible{name}", ref unit.Visible))
                {
                    foreach (var wall in unit.Walls)
                    {
                        wall.ExternalRail.Visible = unit.Visible;
                        foreach (var rail in wall.InternalRails)
                            rail.Visible = unit.Visible;
                    }
                }
                ImGui.SetItemAllowOverlap();
                ImGui.SameLine();

                if (ImGui.Selectable(name, mSelectedUnit == unit))
                {
                    DeselectAll();
                    mSelectedUnit = unit;
                }
                if (expanded)
                {
                    void RailListItem(string type, BGUnitRail rail, int id)
                    {
                        bool isSelected = activeViewport.mEditContext.IsSelected(rail);
                        string wallname = $"{type} {id}";

                        ImGui.Indent();

                        if (ImGui.Checkbox($"##Visible{wallname}", ref rail.Visible))
                        {

                        }
                        ImGui.SameLine();

                        ImGui.Columns(2);

                        void SelectRail()
                        {
                            activeViewport.mEditContext.DeselectAllOfType<BGUnitRail>();

                            activeViewport.mEditContext.Select(rail);

                            //Remove actor properties to show path properties
                            DeselectAll();
                            //Show selection for rail with properties
                            mSelectedUnitRail = rail;
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

                    if (unit == mSelectedUnit)
                    {
                        if (ImGui.BeginPopupContextWindow("RailMenu", ImGuiPopupFlags.MouseButtonRight))
                        {
                            if (ImGui.MenuItem("Add Wall"))
                                unit.Walls.Add(new Wall(unit));

                            if (ImGui.MenuItem($"Remove {name}"))
                                removed_tile_units.Add(unit);

                            ImGui.EndPopup();
                        }
                    }

                    if (ImGui.Button("Add Wall"))
                        unit.Walls.Add(new Wall(unit));
                    ImGui.SameLine();
                    if (ImGui.Button("Remove Wall"))
                    {
                        foreach (var wall in unit.Walls.Where(x => activeViewport.mEditContext.IsSelected(x.ExternalRail)).ToList())
                            unit.Walls.Remove(wall);
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
                    unitHolder.mUnits.Remove(tile);
                removed_tile_units.Clear();
            }
        }

        private void CourseRailsView(CourseRailHolder railHolder)
        {
            foreach(CourseRail rail in railHolder.mRails)
            {
                if (ImGui.TreeNode($"Rail {railHolder.mRails.IndexOf(rail)}"))
                {
                    //ImGui.Checkbox("IsClosed", ref rail.mIsClosed);

                    foreach (CourseRail.CourseRailPoint pnt in rail.mPoints)
                    {
                        if (ImGui.TreeNode($"Point {rail.mPoints.IndexOf(pnt)}"))
                        {
                            ImGui.TreePop();
                        }
                    }

                    ImGui.TreePop();
                    /*if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        
                    }*/
                }
            }
        }

        private void CourseActorsLayerView(CourseActorHolder actorArray)
        {
            if (!mHasFilledLayers)
            {
                FillLayers(actorArray);
            }

            if (ImGui.Checkbox("All Layers", ref mAllLayersVisible))
            {
                foreach (string layer in mLayersVisibility.Keys)
                {
                    mLayersVisibility[layer] = mAllLayersVisible;
                }
            }

            foreach (string layer in mLayersVisibility.Keys)
            {
                bool isVisible = mLayersVisibility[layer];
                if (ImGui.Checkbox("##" + layer, ref isVisible))
                {
                    mLayersVisibility[layer] = isVisible;
                }

                    ImGui.SameLine();

                if (!isVisible)
                {
                    ImGui.BeginDisabled();
                }

                if (ImGui.CollapsingHeader(layer, ImGuiTreeNodeFlags.Selected))
                {
                    ImGui.Indent();
                    ImGui.PushItemWidth(ImGui.GetColumnWidth());
                    if (ImGui.BeginListBox("##" + layer))
                    {
                        foreach (CourseActor actor in actorArray.GetActors())
                        {
                            string actorName = actor.mActorName;
                            string name = actor.mName;
                            ulong actorHash = actor.mActorHash;
                            string actorLayer = actor.mLayer;

                            if (actorLayer != layer)
                            {
                                continue;
                            }

                            bool isSelected = (actor == mSelectedActor);

                            ImGui.PushID(actorHash.ToString());
                            ImGui.Columns(2);
                            if (ImGui.Selectable(actorName, isSelected, ImGuiSelectableFlags.SpanAllColumns))
                            {
                                mSelectedActor = actor;
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
                        ImGui.EndListBox();
                    }
                    ImGui.Unindent();
                }

                if (!isVisible)
                {
                    ImGui.EndDisabled();
                }
            }
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

                ImGui.DragFloat3("##Scale", ref actor.mScale, 0.25f);
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
                List<string> actorParams = ParamDB.GetActorComponents(actor.mActorName);

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
