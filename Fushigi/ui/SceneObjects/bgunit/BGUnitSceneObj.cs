using Fushigi.course;

namespace Fushigi.ui.SceneObjects.bgunit
{
    internal class BGUnitSceneObj(CourseUnit unit) : ISceneObject
    {
        public void Update(ISceneUpdateContext ctx, bool isSelected)
        {
            unit.GenerateTileSubUnits();

            void CreateOrUpdateRail(BGUnitRail rail, bool isBelt = false)
            {
                ctx.UpdateOrCreateObjFor(rail, () => new BGUnitRailSceneObj(unit, rail, isBelt));
            }

            if (unit.mModelType is CourseUnit.ModelType.SemiSolid or CourseUnit.ModelType.Bridge)
            {
                foreach (var rail in unit.mBeltRails)
                    CreateOrUpdateRail(rail, true);
            }

            foreach (var wall in unit.Walls)
            {
                CreateOrUpdateRail(wall.ExternalRail);
                foreach (var rail in wall.InternalRails)
                {
                    CreateOrUpdateRail(rail);
                }
            }
        }
    }
}
