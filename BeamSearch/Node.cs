using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeamSearch
{
    class Node
    {
        public string word;
        public string tag;
        public double score;
        public double PathScore;
        public Node PrevNode;
        public Node ()
        {
            word = "";
            tag = "";
            score = 1;
        }
        public Node (string  w , string T, double sc,double ps)
        {
            word = w;
            tag = T;
            score = sc;
            PathScore = ps;
        }
        public Node (string w, string T, double sc, double ps, Node prev)
        {
            word = w;
            tag = T;
            score = sc;
            PathScore = ps;
            PrevNode = prev;
        }
    }
}
