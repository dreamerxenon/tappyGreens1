using UnityEngine;

[CreateAssetMenu(menuName = "FreeWorld/Course Data")]
public class CourseData : ScriptableObject
{
    public string courseID;
    public GameObject coursePrefab;
    [Range(1, 10)] public int difficulty;
}
