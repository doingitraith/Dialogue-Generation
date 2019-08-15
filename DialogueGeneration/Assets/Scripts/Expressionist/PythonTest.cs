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
    private string requestCode=@"
from productionist import Productionist, ContentRequest

must_have_tags={0}
must_not_have_tags={1}
scoring_metric={2}

request = ContentRequest(must_have=must_have_tags, must_not_have=must_not_have_tags, scoring_metric=scoring_metric)
content_bundle = ""{3}""
dir = ""_ExpressionistExports""
prod = Productionist(content_bundle_name=content_bundle, content_bundle_directory=dir, probabilistic_mode=False, repetition_penalty_mode=True, terse_mode=False, verbosity=1, seed=None)

result = prod.fulfill_content_request(request)
";
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

            PythonEngine.ImportModule("productionist");
        }
    }

    public string GenerateText(string grammarName, List<string> mustHaveTags = null, List<string> mustNotHaveTags = null,
        List<Tuple<string, int>> scoringMetric = null)
    {
        string generatedText = "";

        using (Py.GIL())
        {
            PyDict locals = new PyDict();
            
            PythonEngine.Exec(PrepareRequestCode(grammarName,mustHaveTags,mustNotHaveTags,scoringMetric),
                null, locals.Handle);

            PyObject result = locals.GetItem("result");
            generatedText = result.ToString();
        }
        return generatedText;
    }

    private string PrepareRequestCode(string grammarName, List<string> mustHaveTags = null,
        List<string> mustNotHaveTags = null,
        List<Tuple<string, int>> scoringMetric = null)
    {
        string mustTags = "{";
        if (mustHaveTags != null)
        {
            for (int i = 0; i < mustHaveTags.Count; i++)
            {
                mustTags += "\"" + mustHaveTags[i] + "\"";
                if (i < mustHaveTags.Count - 1)
                    mustTags += ",";
            }
        }
        mustTags += "}";
        
        string mustNotTags = "{";
        if (mustNotHaveTags != null)
        {
            for (int i = 0; i < mustNotHaveTags.Count; i++)
            {
                mustNotTags += "\"" + mustNotHaveTags[i] + "\"";
                if (i < mustNotHaveTags.Count - 1)
                    mustNotTags += ",";
            }
        }
        mustNotTags += "}";

        string scoreMetric = "[";
        if (scoringMetric != null)
        {
            for (int i = 0; i < scoringMetric.Count; i++)
            {
                Tuple<string, int> t = scoringMetric[i];
                scoreMetric += "(\"" + t.Item1 + "\"," + t.Item2 + ")";                
                
                if (i < scoringMetric.Count - 1)
                    scoreMetric += ",";
            }
        }
        scoreMetric += "]";

        string code = string.Format(requestCode, mustTags, mustNotTags, scoreMetric, grammarName);
        return code;
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
