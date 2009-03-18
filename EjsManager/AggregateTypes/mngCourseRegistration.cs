using System;
using System.Collections.Generic;
using System.Text;
using SiliconStudio.Meet.EjsManager.ejsServiceReference;

namespace SiliconStudio.Meet.EjsManager
{
    public class mngCourseRegistration
    {

        ejsCourseRegistration _ejsCourseRegistrationObject;
        public ejsCourseRegistration EjsCourseRegistrationObject
        {
            get { return _ejsCourseRegistrationObject; }
            set { _ejsCourseRegistrationObject = value; }
        }

        ejsUserInfo _userInfoObject;
        public ejsUserInfo UserInfoObject
        {
            get { return _userInfoObject; }
            set { _userInfoObject = value; }
        }

        public int CourseId
        {
            get { return this.EjsCourseRegistrationObject._courseId; }
        }

    }
}
