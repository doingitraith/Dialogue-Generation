using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Expressionist
{
    public struct Sentiment
    {
        public string utterance;
        public float compound;
        public float pos;
        public float neg;
        public float neu;

        public override string ToString()
        {
            return
                $"Positive: {pos * 100.0f}%\nNeutral: {neu * 100.0f}%\nNegative: {neg * 100.0f}%\nOverall: {(compound >= .05 ? "Positive" : compound <= -.05 ? "Negative" : "Neutral")}";
        }

        public bool Equals(Sentiment other)
        {
            return this.utterance.Equals(other.utterance);
        }
    }

    public delegate void DialogueAction();

    public class PythonEndpoint : MonoBehaviour
    {
        private Process _pythonProcess;
        private readonly StringBuilder _inputDataString = new StringBuilder();
        private static bool _isCreated;

        public event DialogueAction OnTextGenerated;
        public event DialogueAction OnSentimentProcessed;
    
        public string currentGeneratedString = "";
        public Sentiment currentSentiment;

        // Start is called before the first frame update
        void Awake()
        {
            if (!_isCreated)
            {
                _isCreated = true;
                DontDestroyOnLoad(this.gameObject);
                
                StartPythonProcess();
                PythonSetupCommands();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void StartPythonProcess()
        {
            _pythonProcess = new Process
            {
                StartInfo =
                {
                    FileName = System.IO.Directory.GetCurrentDirectory() + @"\Python\Python36\python.exe",
                    Arguments = "-i",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            _pythonProcess.OutputDataReceived += PythonOutputReceived;
            _pythonProcess.ErrorDataReceived += PythonErrorReceived;
            _pythonProcess.Start();
            _pythonProcess.BeginOutputReadLine();
            _pythonProcess.BeginErrorReadLine();
        }

        private void PythonErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
        
            if (!e.Data.StartsWith(">>>"))
                Debug.LogError(e.Data);
            //Debug.LogError(e.Data);
        }

        private void PythonOutputReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
        
            if (e.Data.StartsWith("RESULT: "))
            {
                currentGeneratedString = e.Data.Substring(8);
                OnTextGenerated?.Invoke();
            }

            if (e.Data.StartsWith("[SENT]"))
            {
                ParseSentiment(e.Data.Substring(6));
                OnSentimentProcessed?.Invoke();
            }
            //Debug.Log(e.Data);
        }

        private void EnterPythonCode(string input)
        {
            _pythonProcess.StandardInput.Write(input);
            _pythonProcess.StandardInput.Flush();
        }

        private void PythonSetupCommands()
        {
            // Portable python setup
            _inputDataString.AppendLine("import sys");
            _inputDataString.AppendLine("import os");
            //inputDataString.AppendLine("print(os.getcwd())");
            _inputDataString.AppendLine("sys.path.append(os.getcwd()+\"\\Python\\Python36\\Lib\")");
            _inputDataString.AppendLine("sys.path.append(os.getcwd()+\"\\Python\\Python36\\Lib\\site-packages\")");
            //inputDataString.AppendLine("print(sys.path)");
        
            // Expressionist Setup
            _inputDataString.AppendLine("from productionist import Productionist, ContentRequest");
        
            // NLTK Setup
            _inputDataString.AppendLine("import nltk");
            //inputDataString.AppendLine("nltk.download('popular')");
            //inputDataString.AppendLine("nltk.download('vader_lexicon')");
            _inputDataString.AppendLine("from nltk.probability import FreqDist");
            _inputDataString.AppendLine("from nltk.corpus import stopwords");
            _inputDataString.AppendLine("from nltk.stem import PorterStemmer");
            _inputDataString.AppendLine("from nltk.tokenize import sent_tokenize, word_tokenize");
            _inputDataString.AppendLine("from nltk.stem.wordnet import WordNetLemmatizer");
            _inputDataString.AppendLine("from nltk.sentiment.vader import SentimentIntensityAnalyzer");
        
            EnterPythonCode(_inputDataString.ToString());
            _inputDataString.Clear();
        }

        public void ExecuteSentimentAnalysis(string text)
        {
            _inputDataString.AppendLine("text = \"\"\""+text+"\"\"\"");
            _inputDataString.AppendLine("sid = SentimentIntensityAnalyzer()");
            _inputDataString.AppendLine("ss = sid.polarity_scores(text)");
            _inputDataString.AppendLine("print('[SENT]{0};{1};{2};{3};{4}'.format(text, ss['compound'], ss['neg'], ss['neu'], ss['pos']))");
            _inputDataString.AppendLine("print()");
            _inputDataString.AppendLine("");
        
            EnterPythonCode(_inputDataString.ToString());
            _inputDataString.Clear();
        }

        public void ExpressionistRequestCode(string grammarName, List<string> mustHaveTags = null,
            List<string> mustNotHaveTags = null,
            List<Tuple<string, int>> scoringMetric = null, List<Tuple<string, string>> state = null)
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

            string stateStr = "{";
            if (state != null)
            {
                for (int i = 0; i < state.Count; i++)
                {
                    Tuple<string, string> t = state[i];
                    stateStr += "\"" + t.Item1 + "\": \"" + t.Item2 + "\"";
                
                    if (i < state.Count - 1)
                        stateStr += ",";
                }
            }
            stateStr += "}";
        
            string s = "must_have_tags={0}";
            _inputDataString.AppendLine(string.Format(s, mustTags));
            s = "must_not_have_tags={0}";
            _inputDataString.AppendLine(string.Format(s, mustNotTags));
            s = "scoring_metric={0}";
            _inputDataString.AppendLine(string.Format(s, scoreMetric));
            s="state={0}";
            _inputDataString.AppendLine(string.Format(s, stateStr));
            _inputDataString.AppendLine(
                "request = ContentRequest(required_tags=must_have_tags, prohibited_tags=must_not_have_tags, scoring_metric=scoring_metric, state=state, merge_state=True)");
            s = "content_bundle = \"{0}\"";
            _inputDataString.AppendLine(string.Format(s,grammarName));
            _inputDataString.AppendLine("dir = \"_ExpressionistExports\"");
            _inputDataString.AppendLine("prod = Productionist(content_bundle_name=content_bundle, content_bundle_directory=dir, probabilistic_mode=True, repetition_penalty_mode=False, shuffle_candidate_sets=True, terse_mode=False, verbosity=1, seed=None)");
            _inputDataString.AppendLine("result = prod.fulfill_content_request(request)");
            _inputDataString.AppendLine("print(\"RESULT: \"+str(result))");
        
            EnterPythonCode(_inputDataString.ToString());
            _inputDataString.Clear();
        }

        private void ParseSentiment(string s)
        {
            if (s.StartsWith(currentGeneratedString))
            {
                string[] parts = s.Split(';');

                Sentiment sentiment = new Sentiment
                {
                    utterance = parts[0],
                    compound = float.Parse(parts[1]),
                    neg = float.Parse(parts[2]),
                    neu = float.Parse(parts[3]),
                    pos = float.Parse(parts[4])
                };
                currentSentiment = sentiment;
            }
        }

        private void ExitPython()
        {
            if(!_pythonProcess.HasExited)
                _pythonProcess.Kill();
        }


        private void OnApplicationQuit()
        {
            ExitPython();
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}