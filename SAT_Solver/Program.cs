using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

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
            public int gen_count;
            public int variable_count;
            public List<individual> population;
            public int total_flips;

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
                total_flips = 0;
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
                newGen.total_flips = this.total_flips;
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
                    string cur = newGen.population[i].bits;
                    char[] tmp = cur.ToCharArray();
                    if (r.NextDouble() < 0.9)
                    {
                        for(int j = 0; j < this.variable_count; j++)
                        {
                            if (r.NextDouble() < 0.5)
                            {
                                tmp[j] = Flip(tmp[j]);
                                newGen.total_flips++;
                            }
                        }
                        newGen.population[i].bits = new string(tmp);
                    }
                }
                //flip heuristic
                for(int i = 2; i < 10; i++)
                {
                    string cur = newGen.population[i].bits;
                    char[] tmp = cur.ToArray();
                    int[] flip_order = RandomSequence(variable_count);
                    double old_fitness = 0, new_fitness = 0;
                    do
                    {
                        for (int j = 0; j < variable_count; j++)
                        {
                            old_fitness = Get_single_fitness(new string(tmp), cnf);
                            int index = flip_order[j];
                            tmp[index] = Flip(tmp[index]);
                            newGen.total_flips++;
                            new_fitness = Get_single_fitness(new string(tmp), cnf);
                            if (new_fitness < old_fitness)
                            {
                                tmp[index] = Flip(tmp[index]);
                                newGen.total_flips--;
                            }

                        }
                        if (new_fitness == 1)
                        {
                            break;
                        }
                    } while (old_fitness < new_fitness);
                    newGen.population[i].bits = new string(tmp);
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
            string path = AppDomain.CurrentDomain.BaseDirectory + @"\d";
            string[] file_list = Directory.GetFiles(path);
            object synclock = new object();
            int file_count = file_list.Length;
            int total_generations = 0;
            int total_flips = 0;
            int total_success = 0;
            Console.WriteLine(file_list.Length.ToString());

            /*
            foreach(string f in file_list)
            {
                CNF_Instance cnf = Cnf_read(f);

                Generation g = new Generation(cnf.variable_no);
                g.Init();
                Generation newgen = g;
                for (int i = 0; i < 10000; i++)
                //while(true)
                {
                    newgen.Get_fitness(cnf);
                    bool solved = false;
                    for (int j = 0; j < 10; j++)
                    {
                        if (newgen.population[j].fitness == 1)
                        {
                            solved = true;
                            Console.WriteLine("Fuck yeah");
                            break;
                        }
                    }
                    if (solved)
                    {
                        break;
                    }
                    newgen = newgen.NextGen(cnf);
                }
                total_generations += newgen.gen_count;
                total_flips += newgen.total_flips;
            }
            */

            ///*
            int count = 0;
            List<long> running_times = new List<long>();
            Parallel.ForEach(file_list,(f)=> {
                CNF_Instance cnf = Cnf_read(f);

                Generation g = new Generation(cnf.variable_no);
                g.Init();
                Generation newgen = g;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while(true)
                {
                    newgen.Get_fitness(cnf);
                    bool solved = false;
                    for (int j = 0; j < 10; j++)
                    {
                        if (newgen.population[j].fitness == 1)
                        {
                            solved = true;
                            sw.Stop();
                            lock (synclock)
                            {
                                total_success++;
                                running_times.Add(sw.ElapsedMilliseconds);
                            }

                            Console.WriteLine(count.ToString());
                            break;
                        }
                    }
                    if (solved)
                    {
                        break;
                    }
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 60000)
                    {
                        Console.WriteLine(count.ToString() + " Time Out");
                        break;
                    }
                    sw.Start();
                    newgen = newgen.NextGen(cnf);
                }
                lock (synclock)
                {
                    count++;
                    total_generations += newgen.gen_count;
                    total_flips += newgen.total_flips;
                }
                
            });
            //*/
            double avg = (double)total_generations / (double)total_success;
            double avg_f = (double)total_flips / (double)total_success;
            running_times.Sort();
            long median_time = running_times[running_times.Count / 2];
            Console.WriteLine("AVG Generations takes: "+avg.ToString());

        }
    }
}
