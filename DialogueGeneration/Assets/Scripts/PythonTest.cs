using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Python.Runtime;

public class PythonTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
        using (Py.GIL())
        {
            string envPythonHome = System.IO.Directory.GetCurrentDirectory() + @"\Python\Python36";
            string envPythonLib = envPythonHome + @"\Lib";
            
            Environment.SetEnvironmentVariable("PYTHONHOME", envPythonHome, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PATH", envPythonHome + ";" + Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine), EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONPATH", envPythonLib+";"+envPythonLib+@"\site-packages", EnvironmentVariableTarget.Process);

            PythonEngine.PythonHome = envPythonHome;
            PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH");
            
            Debug.Log("PYTHONHOME: "+PythonEngine.PythonHome+" | PYTHONPATH: "+PythonEngine.PythonPath);
            
            StreamReader reader = new StreamReader(Application.dataPath + @"\Scripts\Expressionist\productionist.py");
            string productionistCode = reader.ReadToEnd();

            PyObject productionistScript = PythonEngine.Compile(productionistCode);
            Debug.Log("Python compiled");
            
            PyScope scope = Py.CreateScope("expressionist");

            scope.Execute(productionistScript);
            Debug.Log("Python executed");
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
