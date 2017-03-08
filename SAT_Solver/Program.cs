﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SAT_Solver
{
    class Program
    {

        public static int[] RandomSequence(int length)
        {
            Random r = new Random();
            List<int> generated = new List<int>(length);
            while (generated.Count() < length)
            {
                int cur = r.Next(length);
                if (!generated.Contains(cur))
                {
                    generated.Add(cur);
                }
                else
                {
                    continue;
                }
            }
            return generated.ToArray();
        }

        public static char Flip(char c)
        {
            if (c == '0')
            {
                return '1';
            }
            else
            {
                return '0';
            }
        }

        public static double Get_single_fitness(string bits, CNF_Instance cnf)
        {
            int total = cnf.clause_no;
            int true_count = 0;
            for (int i = 0; i < total; i++)
            {
                bool[] literals = new bool[3];
                for (int j = 0; j < 3; j++)
                {
                    int v = Math.Abs(cnf.clauses[i, j]) - 1;
                    if (bits[v] == '0')
                    {
                        literals[j] = false;
                    }
                    else
                    {
                        literals[j] = true;
                    }

                    if (cnf.clauses[i, j] < 0)
                    {
                        literals[j] = !literals[j];
                    }
                }
                if (literals.Contains(true))
                {
                    true_count++;
                }
            }
            return (double)true_count / (double)total;

        }

        public class Generation
        {
            int gen_count;
            int variable_count;
            public List<individual> population;
            
            public class individual
            {
                public string bits;
                public double fitness;
                public double prob;

                public individual()
                {
                    bits = "";
                    fitness = 0;
                    prob = 0;
                }
            }

            public Generation(int v)
            {
                gen_count = 0;
                variable_count = v;
                population = new List<individual>(10);
            }

            public void Init()
            {
                Random r = new Random();
                for(int i = 0; i < 10; i++)
                {
                    population.Add(new individual());
                    for(int j = 0; j < variable_count; j++)
                    {
                        population[i].bits = string.Concat(population[i].bits, r.Next(2).ToString());
                    }
                }
            }

            public Generation NextGen(CNF_Instance cnf)
            {
                Generation newGen = new Generation(this.variable_count);
                newGen.gen_count = this.gen_count + 1;
                this.Get_fitness(cnf);
                this.population.Sort((x, y) => y.fitness.CompareTo(x.fitness));
                List<individual> selected = new List<individual>(10);
                Random r = new Random();
                //add elites
                for (int i = 0; i < 2; i++)
                {
                    newGen.population.Add(this.population[i]);
                    selected.Add(this.population[i]);
                }

                //select 8
                double[] cdf = new double[10];
                for (int i = 0; i < 10; i++)
                {
                    cdf[i] = this.population[i].prob;
                    if (i != 0)
                    {
                        cdf[i] += cdf[i - 1];
                    }
                }

                for(int i = 0; i < 8; i++)
                {
                    double selector = r.NextDouble();
                    int j = 0;
                    for (j = 0; j < 10; j++)
                    {
                        if (selector <= cdf[j])
                        {
                            break;
                        }
                    }
                    selected.Add(this.population[j]);
                }

                //crossover
                for(int i = 2; i < 10; i += 2)
                {
                    string father = selected[i].bits;
                    string mother = selected[i + 1].bits;
                    individual son = new individual();
                    individual daughter = new individual();
                    for(int j = 0; j < variable_count; j++)
                    {
                        if (r.NextDouble() < 0.5)
                        {
                            son.bits = string.Concat(son.bits, father[j]);
                            daughter.bits = string.Concat(son.bits, mother[j]);
                        }
                        else
                        {
                            daughter.bits = string.Concat(son.bits, father[j]);
                            son.bits = string.Concat(son.bits, mother[j]);
                        }
                    }
                    newGen.population.Add(son);
                    newGen.population.Add(daughter);
                }

                //mutation
                for(int i = 2; i < 10; i++)
                {
                    string cur = this.population[i].bits;
                    char[] tmp = cur.ToCharArray();
                    if (r.NextDouble() < 0.9)
                    {
                        for(int j = 0; j < this.variable_count; j++)
                        {
                            if (r.NextDouble() < 0.5)
                            {
                                tmp[j] = Flip(tmp[j]);
                            }
                        }
                        cur = tmp.ToString();
                    }
                }
                //flip heuristic
                for(int i = 2; i < 10; i++)
                {
                    string cur = this.population[i].bits;
                    char[] tmp = cur.ToArray();
                    int[] flip_order = RandomSequence(variable_count);
                    for(int j = 0; j < variable_count; j++)
                    {
                        double old_fitness, new_fitness;
                        old_fitness = Get_single_fitness(new string(tmp), cnf);
                        int index = flip_order[j];
                        tmp[index] = Flip(tmp[index]);
                        new_fitness = Get_single_fitness(new string(tmp), cnf);
                        if (new_fitness <= old_fitness)
                        {
                            tmp[index] = Flip(tmp[index]);
                        }

                    }
                }


                return newGen;      
            }


            public void Get_fitness(CNF_Instance cnf)
            {
                double sum_prob = 0;
                for(int i = 0; i < 10; i++)
                {
                    int total = cnf.clause_no;
                    int true_count = 0;

                    for(int j = 0; j < total; j++)
                    {
                        bool[] literals = new bool[3];
                        for(int k = 0; k < 3; k++)
                        {
                            string sequence = population[i].bits;
                            int v = Math.Abs(cnf.clauses[j, k])-1;
                            if (sequence[v]=='0')
                            {
                                literals[k] = false;
                            }
                            else
                            {
                                literals[k] = true;
                            }

                            if(cnf.clauses[j, k] < 0)
                            {
                                literals[k] = !literals[k];
                            }
                        }
                        if(literals[0] || literals[1] || literals[2])
                        {
                            true_count++;
                        }
                    }
                    population[i].fitness = (double)true_count / (double)total;
                    sum_prob += population[i].fitness;
                }
                for(int i = 0; i < 10; i++)
                {
                    population[i].prob = population[i].fitness / sum_prob;
                }
            }

        }


        public class CNF_Instance
        {
            public int variable_no;
            public int clause_no;
            public int[,] clauses;
        }

        public static CNF_Instance Cnf_read(string path)
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
            CNF_Instance cnf = Cnf_read(path);

            Generation g = new Generation(cnf.variable_no);
            g.Init();
            g.Get_fitness(cnf);
            g.population.Sort((x,y)=>y.fitness.CompareTo(x.fitness));
            for(int i = 0; i < 10; i++)
            {
                Console.WriteLine(i.ToString() +" "+ g.population[i].bits+" "+g.population[i].fitness.ToString()+" "+g.population[i].prob.ToString());
            }
            Generation newgen = g;
            for(int i=0;i<3000;i++)
            {
                newgen.Get_fitness(cnf);
                bool solved = false;
                for(int j = 0; j < 10; j++)
                {
                    if (newgen.population[j].fitness == 1)
                    {
                        solved = true;
                        break;
                    }
                }
                if (solved)
                {
                    break;
                }
                newgen = newgen.NextGen(cnf);
            }
            newgen.Get_fitness(cnf);
            Console.WriteLine("");

        }
    }
}
