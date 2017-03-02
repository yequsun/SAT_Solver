using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SAT_Solver
{
    class Program
    {
        public class Generation
        {
            int gen_count;
            int variable_count;
            public string[] individuals;
            public double[] fitness;

            public Generation(int v)
            {
                gen_count = 0;
                variable_count = v;
                individuals = new string[10];
                fitness = new double[10];

                for(int i = 0; i < 10; i++)
                {
                    individuals[i] = string.Empty;
                }

            }

            public void init()
            {
                Random r = new Random();
                for(int i = 0; i < 10; i++)
                {
                    for(int j = 0; j < variable_count; j++)
                    {
                        individuals[i] = string.Concat(individuals[i], r.Next(2).ToString());
                    }
                }
            }

            public void get_fitness(CNF_Instance cnf)
            {
                for(int i = 0; i < 10; i++)
                {
                    int total = cnf.clause_no;
                    int true_count = 0;

                    for(int j = 0; j < total; j++)
                    {
                        bool[] literals = new bool[3];
                        for(int k = 0; k < 3; k++)
                        {
                            string sequence = individuals[i];
                            int v = Math.Abs(cnf.clauses[j, k])-1;
                            if (sequence[v]=='0')
                            {
                                literals[k] = false;
                            }
                            else
                            {
                                literals[k] = true;
                            }

                            if(cnf.clauses[j, 0] < 0)
                            {
                                literals[k] = !literals[k];
                            }
                        }
                        if(literals[0] || literals[1] || literals[2])
                        {
                            true_count++;
                        }
                    }
                    this.fitness[i] = (double)true_count / (double)total;
                }
            }

        }


        public class CNF_Instance
        {
            public int variable_no;
            public int clause_no;
            public int[,] clauses;
        }

        public static CNF_Instance cnf_read(string path)
        {
            if (!File.Exists(path)){
                return null;
            }

            string[] text = File.ReadAllLines(path);

            int clause_no = 0;
            int variable_no = 0;

            CNF_Instance cnf = new CNF_Instance();

            int j;
            for (j = 0; j < text.Length; j++)
            {
                string line = text[j];
                if (line[0] == 'c')
                {
                    continue;
                }

                if (line[0] == 'p')
                {
                    string[] tokens = line.Split(default(Char[]), StringSplitOptions.RemoveEmptyEntries);
                    variable_no = Convert.ToInt32(tokens[2]);
                    clause_no = Convert.ToInt32(tokens[3]);
                    break;
                }
            }
            j++;

            cnf.variable_no = variable_no;
            cnf.clause_no = clause_no;
            cnf.clauses = new int[clause_no, 3];

            for(int i = 0; i < clause_no; i++)
            {
                string line = text[i+j];
                string[] tokens = line.Split(default(Char[]), StringSplitOptions.RemoveEmptyEntries);

                cnf.clauses[i, 0] = Convert.ToInt32(tokens[0]);
                cnf.clauses[i, 1] = Convert.ToInt32(tokens[1]);
                cnf.clauses[i, 2] = Convert.ToInt32(tokens[2]);

            }

            return cnf;
        }

        static void Main(string[] args)
        {
            //Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory+ "uf20-01.cnf");
            string path = AppDomain.CurrentDomain.BaseDirectory + "uf20-01.cnf";
            CNF_Instance cnf = cnf_read(path);

            Generation g = new Generation(cnf.variable_no);
            g.init();
            g.get_fitness(cnf);
            
            for(int i = 0; i < 10; i++)
            {
                Console.WriteLine(i.ToString() +" "+ g.individuals[i]+" "+g.fitness[i].ToString());
            }


        }
    }
}
