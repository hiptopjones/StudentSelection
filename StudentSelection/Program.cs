using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentSelection
{
    class Student
    {
        public string Name { get; set; }
        public List<int> Choices { get; set; }

        public Student()
        {
            Choices = new List<int>();
        }
    }

    class Program
    {
        static Random Random = new Random();

        static void Main(string[] args)
        {
            int studentCount = 144;
            int projectCount = 24;
            int maxStudentsPerProject = 6;

            List<Student> students = null;

            if (args.Length > 0)
            {
                string filePath = args[0];
                students = LoadStudents(filePath);
            }
            else
            {
                students = GenerateStudents(studentCount, projectCount);
            }

            Dictionary<int, List<Student>> projectAssignments = AssignStudents(students, maxStudentsPerProject);
            DumpAssignments(projectAssignments);

            PromptToContinue();
        }

        static void PromptToContinue()
        {
            Console.WriteLine("Hit a key to continue...");
            Console.ReadKey();
        }

        static void DumpAssignments(Dictionary<int, List<Student>> projectAssignments)
        {
            Console.WriteLine("Assignments:");
            List<int> sortedProjectIds = projectAssignments.Keys.ToList();
            sortedProjectIds.Sort();

            foreach (int projectId in sortedProjectIds)
            {
                List<Student> assignedStudents = projectAssignments[projectId];

                Console.WriteLine($"   Project {projectId} ({assignedStudents.Count} students)");

                foreach (Student student in projectAssignments[projectId])
                {
                    Console.WriteLine($"      {student.Name}");
                }
            }
        }

        static List<Student> LoadStudents(string filePath)
        {
            List<Student> students = new List<Student>();

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: '{filePath}'");
                return students;
            }

            // Load the file and skip the header row
            IEnumerable<string> lines = File.ReadAllLines(filePath).Skip(1);

            foreach (string line in lines)
            {
                string[] fields = line.Split(',');
                Student student = new Student { Name = fields[0] };
                for (int i = 1; i < fields.Length; i++)
                {
                    string value = fields[i];
                    if (!string.IsNullOrEmpty(value) && value != "#N/A")
                    {
                        student.Choices.Add(Int32.Parse(fields[i]));
                    }
                }

                students.Add(student);
            }

            return students;
        }

        static List<Student> GenerateStudents(int studentCount, int projectCount)
        {
            List<Student> students = new List<Student>();

            for (int i = 0; i < studentCount; i++)
            {
                Student student = new Student { Name = GenerateName(i), Choices = GenerateChoices(projectCount) };
                students.Add(student);
            }

            return students;
        }

        static string GenerateName(int i)
        {
            return $"Student {i}";
        }

        static List<int> GenerateChoices(int projectCount)
        {
            List<int> choices = new List<int>();

            while (choices.Count < 3)
            {
                int choice = Random.Next(projectCount);
                if (!choices.Contains(choice))
                {
                    choices.Add(choice);
                }
            }

            return choices;
        }

        static Dictionary<int, List<Student>> AssignStudents(List<Student> availableStudents, int maxStudentsPerProject)
        {
            Dictionary<int, List<Student>> projectAssignments = new Dictionary<int, List<Student>>();
            List<Student> unassignedStudents = new List<Student>();

            Console.WriteLine($"Attempt to assign students to projects...");

            Console.WriteLine($"Students remaining: {availableStudents.Count}");
            if (availableStudents.Count > 0)
            {
                Console.WriteLine($"Filling projects that have no more than {maxStudentsPerProject} requests...");

                // projectId-centric:
                // Step 1: Identify number of requests for each projectId
                // Step 2: Step through projects in order of ascending requests (assign projects with the least requests first)

                // Build map of projects to all requesting students
                Dictionary<int, List<Student>> projectRequests = new Dictionary<int, List<Student>>();
                foreach (Student student in availableStudents)
                {
                    foreach (int projectId in student.Choices)
                    {
                        List<Student> requestingStudents;
                        if (!projectRequests.TryGetValue(projectId, out requestingStudents))
                        {
                            requestingStudents = new List<Student>();
                            projectRequests[projectId] = requestingStudents;
                        }

                        requestingStudents.Add(student);
                    }
                }

                // Enumerate the map by visiting the projects with fewest requests first
                foreach (KeyValuePair<int, List<Student>> pair in projectRequests.OrderBy(x => x.Value.Count))
                {
                    int projectId = pair.Key;
                    List<Student> requestingStudents = pair.Value;

                    // Stop assigning this way when projects have more requests than available spots
                    if (requestingStudents.Count > maxStudentsPerProject)
                    {
                        break;
                    }

                    foreach (Student student in requestingStudents)
                    {
                        // Ensure this student has not been assigned
                        if (!availableStudents.Contains(student))
                        {
                            continue;
                        }

                        // Try to add the student to the project, if the project is not full
                        if (!AddStudentToProjectList(projectId, student, projectAssignments, maxStudentsPerProject))
                        {
                            break;
                        }

                        // This student is assigned, so remove it from consideration
                        availableStudents.Remove(student);
                    }
                }

                Console.WriteLine($"Students remaining: {availableStudents.Count}");
            }

            if (availableStudents.Count > 0)
            {
            Console.WriteLine($"Filling projects in order of student preference...");

            // Student-centric:
            // Step 1: Step through students in random order
            // Step 2: Assign to highest-priority projectId if possible, second highest-priority projectId if not, etc.
            // Step 3: If student cannot be assigned, add to the unassigned list
            // Step 4: Step through the unassigned list
            // Step 5: For each request made by an unassigned student, see if a student with one of those assignments could be bumped

            for (int i = availableStudents.Count - 1; i >= 0; i--)
            {
                Student student = availableStudents[i];

                if (AssignStudent(student, projectAssignments, maxStudentsPerProject))
                {
                    availableStudents.Remove(student);
                }
            }

            Console.WriteLine($"Students remaining: {availableStudents.Count}");

                if (availableStudents.Count > 0)
                {
                    Console.WriteLine($"Making changes to try and assign remaining students...");

                    for (int i = availableStudents.Count - 1; i >= 0; i--)
                    {
                        Student student = availableStudents[i];

                        HashSet<int> visitedProjects = new HashSet<int>();

                        if (AssignStudent(student, projectAssignments, maxStudentsPerProject, visitedProjects, depthToRecurse: 4))
                        {
                            availableStudents.Remove(student);
                        }
                    }

                    Console.WriteLine($"Students remaining: {availableStudents.Count}");
                }

                if (availableStudents.Count > 0)
                {
                    Console.WriteLine($"Unable to assign the following students:");
                    Console.WriteLine(string.Join("\n", availableStudents.Select(x => "   " + x.Name)));
                }
            }

            return projectAssignments;
        }
        
        static bool AssignStudent(Student unassignedStudent, Dictionary<int, List<Student>> projectAssignments, int maxStudentsPerProject, HashSet<int> visitedProjects = null, int depthToRecurse = 0)
        {
            if (visitedProjects == null)
            {
                visitedProjects = new HashSet<int>();
            }

            if (visitedProjects.Count > depthToRecurse)
            {
                return false;
            }

            foreach (int projectId in unassignedStudent.Choices)
            {
                // Ensure we don't evaluate a projectId twice
                if (visitedProjects.Contains(projectId))
                {
                    continue;
                }

                if (AddStudentToProjectList(projectId, unassignedStudent, projectAssignments, maxStudentsPerProject))
                {
                    return true;
                }

                List<Student> assignedStudents = projectAssignments[projectId];
                foreach (Student student in assignedStudents)
                {
                    HashSet<int> copyOfVisitedProjects = new HashSet<int>(visitedProjects);
                    copyOfVisitedProjects.Add(projectId);

                    if (AssignStudent(student, projectAssignments, maxStudentsPerProject, copyOfVisitedProjects, depthToRecurse))
                    {
                        assignedStudents.Remove(student);
                        assignedStudents.Add(unassignedStudent);
                        return true;
                    }
                }
            }

            return false;
        }

        static bool AddStudentToProjectList(int projectId, Student student, Dictionary<int, List<Student>> projectAssignments, int maxStudentsPerProject)
        {
            List<Student> assignedStudents;
            if (!projectAssignments.TryGetValue(projectId, out assignedStudents))
            {
                assignedStudents = new List<Student>();
                projectAssignments[projectId] = assignedStudents;
            }

            if (assignedStudents.Count < maxStudentsPerProject)
            {
                assignedStudents.Add(student);
                return true;
            }

            return false;
        }
    }
}
