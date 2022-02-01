using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MemoryLeakDemo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            IList<Company<Employee>> companies = new List<Company<Employee>>();
            var location = new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath;
            var path = Path.GetDirectoryName(location);
            var filePath = args.Any() ? args[0] : Path.Combine(path, "Resource\\Companydata.txt");
            var mainStream = new StreamWriter(filePath);

            var i = 0;
            // 1. memory leak : Create steam for each company but not disposed.   
            // 2. event subscribed not released.                 
            while (i++ <= 10000)
            {
                foreach (var item in new List<string> { "DX Generation", "Nxt", "Dudley Boyz" })
                {
                    using var company = new Company<Employee>(item);
                    company.OnNewJoinne += e =>
                    {
                        CompanyOnNewJoinne(e, mainStream);
                    };
                    foreach (var item1 in new List<string> { "employe1", "employe2" })
                    {
                        company.AddNewEmployee(item1);
                    }

                    companies.Add(company);
                }
            }

            mainStream.Dispose();
            Console.ReadKey();
        }

        private static void CompanyOnNewJoinne(EmployeeJoinedEventArgs e, StreamWriter stream)
        {
            Console.WriteLine($"{e.EmployeeName} employee Joined {e.EmployerName}");
            stream.WriteLine($"{e.EmployeeName} employee Joined {e.EmployerName}");
        }
    }

    public class Company<T> : IDisposable where T : Employee
    {
        public delegate void EmployeJoined(EmployeeJoinedEventArgs name);
        public event EmployeJoined OnNewJoinne;
        public string Name { get; private set; }
        public StreamWriter FileStream { get; private set; }
        private readonly IList<T> _employee = new List<T>();

        private bool _disposed;

        public Company(string name)
        {
            this.Name = name;
            var location = new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath;
            var path = Path.GetDirectoryName(location);
            // each object have fileStream but no dispose.  
            // you can fix it via Idispose implementation to dispose FileStream  
            this.FileStream = File.CreateText(Path.Combine(path, Path.GetRandomFileName() + ".txt"));
        }

        public bool AddNewEmployee(string name)
        {
            if (_employee.FirstOrDefault(s => s.Name == name) != null)
            {
                return false;
            }

            _employee.Add((T)Activator.CreateInstance(typeof(T), name));

            // Write in local stream.  
            FileStream.WriteLine($"{name} employee Joined {this.Name}");

            // write in main stream.  
            OnNewJoinne?.Invoke(new EmployeeJoinedEventArgs(this.Name, name));
            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.  
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                FileStream?.Dispose();
                OnNewJoinne = null;
            }

            _disposed = true;
        }
    }

    public class Employee
    {
        public string Name { get; private set; }
        public Employee(string name)
        {
            this.Name = name;
        }
    }

    public class EmployeeJoinedEventArgs : EventArgs
    {
        public EmployeeJoinedEventArgs(string companyName, string employeeName)
        {
            EmployerName = companyName;
            EmployeeName = employeeName;
        }

        public string EmployerName { get; private set; }
        public string EmployeeName { get; private set; }
    }
}
