using Fushigi.course;
using Fushigi.ui.SceneObjects.bgunit;

namespace Fushigi.ui.SceneObjects
{
    class CourseAreaSceneRoot(CourseArea area) : ISceneRoot
    {
        public void Update(ISceneUpdateContext ctx)
        {
            //call ctx.UpdateOrCreateObjFor()
            //for every object (actor/rail/etc.) that should be part of the scene
            //the scene object classes for these objects should go in SceneObjects

            foreach (var unit in area.mUnitHolder.mUnits)
            {
                ctx.UpdateOrCreateObjFor(unit, () => new BGUnitSceneObj(unit));
            }
        }
    }


}
