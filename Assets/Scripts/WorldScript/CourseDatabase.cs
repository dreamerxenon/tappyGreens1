using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "FreeWorld/Course Database")]
public class CourseDatabase : ScriptableObject
{
    public List<CourseData> allCourses;
}
