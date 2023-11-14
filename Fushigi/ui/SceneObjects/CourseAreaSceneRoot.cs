using Fushigi.course;

namespace Fushigi.ui.SceneObjects
{
    class CourseAreaSceneRoot(CourseArea area) : ISceneRoot
    {
        public void Update(ISceneUpdateContext ctx)
        {
            //call ctx.UpdateOrCreateObjFor()
            //for every object (actor/rail/etc.) that should be part of the scene
            //the scene object classes for these objects should go in SceneObjects
        }
    }


}
