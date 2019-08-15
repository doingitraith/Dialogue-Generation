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
            
            //Debug.Log("PYTHONHOME: "+PythonEngine.PythonHome+" | PYTHONPATH: "+PythonEngine.PythonPath);
            
            /*
            StreamReader reader = new StreamReader(Application.dataPath + @"\Scripts\Expressionist\productionist.py");
            string productionistCode = reader.ReadToEnd();

            PyObject productionistScript = PythonEngine.Compile(productionistCode);
            Debug.Log("Python compiled");
            
            PyScope scope = Py.CreateScope("expressionist");

            scope.Execute(productionistScript);
            Debug.Log("Python executed");
            */

            //PythonEngine.ImportModule("productionist");

            PyDict locals = new PyDict();
            
            PythonEngine.Exec(@"
from productionist import Productionist, ContentRequest

must_have_tags = {}
must_not_have_tags= {}
scoring_metric=[]

request = ContentRequest(must_have=must_have_tags, must_not_have=must_not_have_tags, scoring_metric=scoring_metric)
content_bundle = ""introduction""
dir = ""_ExpressionistExports""
prod = Productionist(content_bundle_name=content_bundle, content_bundle_directory=dir, probabilistic_mode=False, repetition_penalty_mode=True, terse_mode=False, verbosity=1, seed=None)

result = prod.fulfill_content_request(request)
", null, locals.Handle);

            PyObject result = locals.GetItem("result");
            Debug.Log(result.ToString());

        }
        
    }


    private void OnApplicationQuit()
    {
        PythonEngine.Shutdown();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
