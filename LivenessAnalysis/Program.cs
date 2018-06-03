using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LivenessAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            // Input Format:
            // {SUCC \n USE \n DEF \n ISMOVE}* \n InitialLiveIn \n FinalLiveOut
            //Console.WriteLine("Welcome to WTF live-in and live-out analyzer!\n");
            //if (args.Length != 1)
            //{
            //    Console.WriteLine("Please drag the input file to this program!");
            //}
            //string[] rawFile = File.ReadAllLines(args[0]);
            string[] rawFile = File.ReadAllLines("F:\\文档\\大学课程材料\\编译原理\\HW\\LivenessAnalysis\\LivenessAnalysis\\bin\\Debug\\input2.txt");
            if (rawFile.Length % 4 != 2)
            {
                Console.WriteLine("Input Error");
            }
            string initLiveIn = rawFile[rawFile.Length - 2];
            string finalLiveOut = rawFile[rawFile.Length - 1];
            // Parse input
            List<HashSet<int>> succ = new List<HashSet<int>>().Append(new HashSet<int>(new HashSet<int>().Append(1))).ToList();
            List<HashSet<string>> use = new List<HashSet<string>>().Append(new HashSet<string>()).ToList();
            List<HashSet<string>> def = new List<HashSet<string>>().Append(new HashSet<string>()).ToList();
            List<bool> isMove = new List<bool>().Append(false).ToList();
            HashSet<string> identifier = new HashSet<string>();
            for (int i = 0; i < rawFile.Length / 4; i++)
            {
                HashSet<int> curSucc = new HashSet<int>();
                if (rawFile[i * 4].Length > 0)
                    curSucc.UnionWith(rawFile[i * 4].Split(',').Select(s => int.Parse(s)));
                HashSet<string> curUse = new HashSet<string>();
                if (rawFile[i * 4 + 1].Length > 0)
                    curUse.UnionWith(rawFile[i * 4 + 1].Split(','));
                HashSet<string> curDef = new HashSet<string>();
                if (rawFile[i * 4 + 2].Length > 0)
                    curDef.UnionWith(rawFile[i * 4 + 2].Split(','));
                bool curIsMove = int.Parse(rawFile[i * 4 + 3]) != 0 ? true : false;

                identifier.UnionWith(curUse);
                identifier.UnionWith(curDef);

                succ.Add(curSucc);
                use.Add(curUse);
                def.Add(curDef);
                isMove.Add(curIsMove);
            }
            succ.Add(new HashSet<int>(new HashSet<int>().Append(-1)));
            use.Add(new HashSet<string>());
            def.Add(new HashSet<string>());
            isMove.Add(false);

            // Get live-in and live-out
            int instCount = succ.Count;
            if (succ[instCount - 2].Count == 1 && (succ[instCount - 2].ToList()[0] == -1))
            {
                succ[instCount - 2] = new HashSet<int>(new HashSet<int>().Append(instCount - 1));
            }
            else
            {
                succ[instCount - 2].UnionWith(new HashSet<int>(new HashSet<int>().Append(instCount - 1)));
            }
            HashSet<string>[] liveIn = new HashSet<string>[instCount];
            HashSet<string>[] liveOut = new HashSet<string>[instCount];
            for (int i = 0; i < instCount; i++)
            {
                liveIn[i] = new HashSet<string>();
                liveOut[i] = new HashSet<string>();
            }
            liveIn[0] = new HashSet<string>();
            if (initLiveIn.Length > 0)
                liveIn[0].UnionWith(initLiveIn.Split(','));
            liveOut[instCount - 1] = new HashSet<string>();
            if (finalLiveOut.Length > 0)
                liveIn[instCount - 1].UnionWith(finalLiveOut.Split(','));
            bool change = false;
            do
            {
                change = false;
                for (int i = 1; i < instCount - 1; i++)
                {
                    HashSet<string> tIn = new HashSet<string>(liveIn[i]);
                    HashSet<string> tOut = new HashSet<string>(liveOut[i]);
                    liveIn[i] = new HashSet<string>(use[i].Union(liveOut[i].Except(def[i])));
                    liveOut[i] = new HashSet<string>();
                    foreach (int x in succ[i])
                    {
                        if (x == -1) break;
                        liveOut[i].UnionWith(liveIn[x]);
                    }
                    if ((!liveIn[i].SetEquals(tIn)) || (!liveOut[i].SetEquals(tOut)))
                        change = true;
                }
            } while (change);
            Console.WriteLine("Inst\tLiveIn\tLiveOut");
            for (int i = 1; i < instCount - 1; i++)
            {
                Console.WriteLine(i.ToString() + '\t' + String.Join(",", liveIn[i].ToArray()) + '\t' + String.Join(",", liveOut[i].ToArray()));
            }

            // Interfere Graph
            List<string> idList = new List<string>(identifier.ToList());
            var interfere = Enumerable.Range(0, identifier.Count).Select(x => Enumerable.Repeat(0, identifier.Count).ToArray()).ToArray();
            HashSet<string> currentLive = new HashSet<string>();
            for (int i = 1; i < instCount - 1; i++)
            {
                // LiveIn
                foreach (var x in liveIn[i])
                    foreach (var y in liveIn[i])
                    {
                        var idx1 = idList.IndexOf(x);
                        var idx2 = idList.IndexOf(y);
                        interfere[idx1][idx2] = interfere[idx2][idx1] = 1;
                    }

                // Def
                foreach (var x in def[i])
                {
                    var idx1 = idList.IndexOf(x);
                    int idxMov;
                    if (isMove[i])
                        idxMov = idList.IndexOf(use[i].ToList()[0]);
                    else
                        idxMov = -1;
                    foreach (var y in liveOut[i])
                    {
                        var idx2 = idList.IndexOf(y);
                        if (idx2 != idxMov)
                        {
                            interfere[idx1][idx2] = 1;
                            interfere[idx2][idx1] = 1;
                        }
                    }
                }

                if (isMove[i])
                {
                    var idx1 = idList.IndexOf(def[i].ToList()[0]);
                    var idx2 = idList.IndexOf(use[i].ToList()[0]);
                    interfere[idx1][idx2] = interfere[idx2][idx1] = -1;
                }
            }
            Console.WriteLine("\nInterfere Matrix");
            Console.Write(" \t");
            foreach (var x in idList)
            {
                Console.Write(x + '\t');
            }
            Console.WriteLine();
            for (int i = 0; i < idList.Count; i++)
            {
                Console.Write(idList[i] + '\t');
                for (int j = 0; j < idList.Count; j++)
                    Console.Write(interfere[i][j].ToString() + '\t');
                Console.WriteLine();
            }

            Console.WriteLine("\nInterfere List");
            for (int i = 0; i < idList.Count; i++)
            {
                Console.Write(idList[i] + " :\t");
                for (int j = 0; j < idList.Count; j++)
                    if (interfere[i][j] == 1 && i != j)
                        Console.Write(idList[j] + '\t');
                Console.WriteLine();
            }

            Console.WriteLine("\nPress any key to exit");
            Console.ReadKey();
        }
    }
}
