using Fushigi.course;

namespace Fushigi.ui.SceneObjects.bgunit
{
    internal class BGUnitSceneObj(CourseUnit unit) : ISceneObject
    {
        public void Update(ISceneUpdateContext ctx, bool isSelected)
        {
            unit.GenerateTileSubUnits();

            void CreateOrUpdateRail(BGUnitRail rail)
            {
                ctx.UpdateOrCreateObjFor(rail, () => new BGUnitRailSceneObj(unit, rail));
            }

            foreach (var wall in unit.Walls)
            {
                CreateOrUpdateRail(wall.ExternalRail);
                foreach (var rail in wall.InternalRails)
                {
                    CreateOrUpdateRail(rail);
                }
            }

            //Don't include belt for now. TODO how should this be handled?
            //foreach (var rail in unit.mBeltRails)
            //    CreateOrUpdateRail(rail);
        }
    }
}
