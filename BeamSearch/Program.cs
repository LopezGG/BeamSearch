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
            List<String> TagList = new List<string>();
            //Reads Model File and builds the required DS
            ReadModelFile(ClassFeatureDict, WordClassDict, ModelFile, TopN,TagList);
            
            List<int> LineBoundaries = new List<int>();
            ReadBoundaryFile(BoundaryPath, LineBoundaries);


            // we are reading test data from here on
            string line,  prevT="", prev2Tag="";
            int docCount = 0,BoundaryValue=0,curWordNum=1;
            double Correct = 0,totalLines=0;
            double score = 0;

            List<Node> Parent = new List<Node>();
            StreamWriter Sw = new StreamWriter(OutFile);
            Sw.WriteLine("%%%%% test data:");
            using(StreamReader Sr = new StreamReader(testPath))
            {
                while ((line = Sr.ReadLine()) != null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    //score refers to the prob value for this node. it has to be set to zero for every word
                    score = 0;
                    totalLines++;
                    List<Node> CurCandidates = new List<Node>();
                    if(curWordNum > BoundaryValue)
                    {
                        // this means we have moved to a new training instance
                        BoundaryValue = LineBoundaries[docCount++];
                        curWordNum = 1;
                        Node Root = new Node("BOS", "BOS", 1,0);
                        //parents from previous test instance can be cleared when we are in a new one
                        Parent.Clear();
                        Parent.Add(Root);

                    }
                    string[] words = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    List<String> Features = new List<string>();
                    string curWord = "",word="";
                    double maxScore = double.MinValue;
                    //TODO : indexed from 2 because we dont want the instance name and gold standard for now
                    for (int i = 2; i < words.Length; i++)
                    {
                        if (words[i].Contains("curW="))
                        {
                            curWord = words[i];
                            word = curWord.Substring(curWord.IndexOf("=")+1);
                        }
                        //TODO:Check if feature Value is zero
                        if (words[i].Trim() !="1")    
                            Features.Add(words[i]);
                    }
                    if (String.IsNullOrWhiteSpace(curWord))
                    {
                        word = words[0].Substring(words[0].LastIndexOf("-") + 1);
                        curWord = "curW=" + word;
                    }
                    curWord = curWord.Trim();   
                    //this is for storign previous tags
                    List<string> AddFeatures = new List<string>();
                    List<String> ClassList = new List<string>();
                    //if we have not seen the current word in the test instance we just continue
                    if (!WordClassDict.ContainsKey(curWord))
                    {
                        ClassList = TagList;
                    }
                    else
                    {
                        ClassList = WordClassDict[curWord].Keys.ToList();
                    }
                    //this is to help us find prob.
                    double totalScoreAcrossAllPaths = 0;    
                    foreach (var nd in Parent)
                    {
                        AddFeatures.Clear();
                        prevT = nd.tag;
                        if (nd.PrevNode != null)
                            prev2Tag = nd.PrevNode.tag;
                        else
                            prev2Tag = "BOS";
                        
                        AddFeatures.Add("prevT=" + prevT);
                        AddFeatures.Add("prevTwoTags=" + prev2Tag + "+" + prevT);
                        //this loop handles topN condition

                        foreach (var classLabel in ClassList)
                        {
                            score = 0;
                            var ClassLambda = ClassFeatureDict[classLabel];

                            //we are goign to get the value for this path
                            foreach (var feat in Features)
                            {
                                if (ClassLambda.ContainsKey(feat))
                                {
                                    score += ClassLambda[feat];
                                }
                                    
                            }
                            // this is to account for prev tags
                            foreach (var feat in AddFeatures)
                            {
                                if (ClassLambda.ContainsKey(feat))
                                    score += ClassLambda[feat];
                            }
                            score += ClassLambda["<default>"];
                            score = System.Math.Exp(score);
                            
                            
                            totalScoreAcrossAllPaths += score;
                            //we will update the pathScore later
                            Node Cand = new Node(word, classLabel, score,0,nd);
                            CurCandidates.Add(Cand);
                        } 
                    }
                    
                    foreach (var cd in CurCandidates)
                    {
                        cd.score = cd.score / totalScoreAcrossAllPaths;
                        cd.PathScore = System.Math.Log10(cd.score) + (cd.PrevNode.PathScore);
                        //this will help in pruning by beam size
                        if (cd.PathScore > maxScore)
                            maxScore = cd.PathScore;
                            
                        
                    }

                    //update parents for the next word
                    Parent.Clear();
                    //Order by the whole path score and just take the topK candidate. We will prune further with beam.
                    CurCandidates = CurCandidates.OrderByDescending(x => x.PathScore).Take(TopK).ToList();

                    foreach (var cd in CurCandidates)
                    {

                        if(cd.PathScore == maxScore)
                        {
                            Sw.WriteLine(words[0]+" "+ words[1] +" " +cd.tag+" "+cd.score);
                            if (words[1] == cd.tag)
                                Correct++;
                        }
                            
                        if (cd.PathScore + BeamSize >= maxScore)
                            Parent.Add(cd);
                        else
                            break;
                    }

                    curWordNum++;

                }
            }
            Console.WriteLine("Accuracy : " + Convert.ToString(Correct/totalLines));
            Sw.Close();
            Console.ReadLine();
        }
        public static void ReadBoundaryFile (string BoundaryPath, List<int> LineBoundaries)
        {
            string line;
            using (StreamReader Sr = new StreamReader(BoundaryPath))
            {
                while ((line = Sr.ReadLine()) != null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    LineBoundaries.Add(Convert.ToInt32(line));
                }
            }
        }
        public static void ReadModelFile (Dictionary<string, Dictionary<string, double>> ClassFeatureDict, Dictionary<string, Dictionary<string, double>> WordClassDict, string ModelFile, int TopN, List<String> TagList)
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
                        classLabel=classLabel.Trim();
                        TagList.Add(classLabel);
                        if (!ClassFeatureDict.ContainsKey(classLabel))
                            ClassFeatureDict.Add(classLabel, new Dictionary<string, double>());
                        continue;
                    }
                    string[] words = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length != 2)
                        throw new Exception("incorrect format in modelFile");
                    key = words[0];
                    key = key.Trim();
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

            TagList = TagList.Distinct().ToList();
        }
    }
}
