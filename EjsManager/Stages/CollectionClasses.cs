using System;
using System.Collections.Generic;
using System.Text;
using SiliconStudio.Meet.EjsManager.ejsServiceReference;
using System.Collections.ObjectModel;

namespace SiliconStudio.Meet.EjsManager
{
	internal class ObservableCourseList : ObservableCollection<ejsCourse> { }
	internal class ObservableUserList : ObservableCollection<ejsUserInfo> { }
	internal class ObservableAssignmentList : ObservableCollection<ejsAssignment> { }
	internal class ObservableCourseDocumentList : ObservableCollection<ejsCourseDocument> { }
    internal class ObservableCourseRegistrationList : ObservableCollection<mngCourseRegistration> { }
}
