using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeamSearch
{
    class Program
    {
        static void Main (string[] args)
        {
            string testPath = args[0];
            string BoundaryPath = args[1];
            string ModelFile = args[2];
            string OutFile = args[3];
            double BeamSize = Convert.ToDouble(args[4]);
            int TopN = Convert.ToInt32(args[5]);
            int TopK = Convert.ToInt32(args[6]);
            Dictionary<string, Dictionary<string, double>> ClassFeatureDict = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<string, Dictionary<string, double>> WordClassDict = new Dictionary<string, Dictionary<string, double>>();
            //Reads Model File and builds the required DS
            ReadModelFile(ClassFeatureDict, WordClassDict, ModelFile, TopN);
            
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static void ReadModelFile (Dictionary<string, Dictionary<string, double>> ClassFeatureDict, Dictionary<string, Dictionary<string, double>> WordClassDict, string ModelFile, int TopN)
        {
            string line, classLabel = "", key = "";
            double value = 0;
            using (StreamReader Sr = new StreamReader(ModelFile))
            {
                Dictionary<string, Dictionary<string, double>> WordClassDictTemp = new Dictionary<string, Dictionary<string, double>>();
                while ((line = Sr.ReadLine()) != null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    if (line.Contains("FEATURES FOR CLASS"))
                    {
                        classLabel = line.Substring(line.IndexOf("CLASS") + 5);
                        if (!ClassFeatureDict.ContainsKey(classLabel))
                            ClassFeatureDict.Add(classLabel, new Dictionary<string, double>());
                        continue;
                    }
                    string[] words = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length != 2)
                        throw new Exception("incorrect format in modelFile");
                    key = words[0];
                    value = Convert.ToDouble(words[1]);
                    // this will help us with gettign topN
                    if (key.Contains("curW"))
                    {
                        if (WordClassDictTemp.ContainsKey(key) && WordClassDictTemp[key].ContainsKey(classLabel))
                            WordClassDictTemp[key][classLabel] += value;
                        else if (WordClassDictTemp.ContainsKey(key))
                            WordClassDictTemp[key].Add(classLabel, value);
                        else
                        {
                            Dictionary<string, double> temp = new Dictionary<string, double>();
                            temp.Add(classLabel, value);
                            WordClassDictTemp.Add(key, temp);
                        }

                    }

                    // Now we will create a dictionary for each class and word in it
                    if (ClassFeatureDict.ContainsKey(classLabel) && ClassFeatureDict[classLabel].ContainsKey(key))
                        ClassFeatureDict[classLabel][key] += value;
                    else if (ClassFeatureDict.ContainsKey(classLabel))
                        ClassFeatureDict[classLabel].Add(key, value);
                    else
                    {
                        Dictionary<string, double> temp1 = new Dictionary<string, double>();
                        temp1.Add(key, value);
                        ClassFeatureDict.Add(classLabel, temp1);
                    }

                }
                //now we are going to sort the wordClass Dict to retain only topN classes for each word
                foreach (var wordClassList in WordClassDictTemp)
                {
                    var Wc = wordClassList.Value.OrderByDescending(x => x.Value).Take(TopN).ToDictionary(x => x.Key, x => x.Value);
                    WordClassDict.Add(wordClassList.Key, Wc);
                }
            }
        }
    }
}
