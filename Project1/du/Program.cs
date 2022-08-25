/*
 * The purpose of this program is to create three different scenarios at which the du command will be recreated.
 * The three scenarios are in parallel (threaded) mode, sequential mode, and both parallel and sequential. The files
 * will be found recursively. Only the count of the folders, files, and total byte size will be printed in the end
 * to reduce the cost of printing each file line-by-line. Parallel mode will use a parallel foreach loop and the
 * sequential mode will use a regular foreach loop. The code will be formatted in proper OOP formatting.
 *
 * @author Giovanni Coppola (gac6151@rit.edu)
 */

using System.Diagnostics;

namespace du
{
    /// <summary>
    /// The Program class will be what processes the command line arguments and calls the proper functions to run
    /// the du command in the proper way. 
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main function will process the command line arguments and call the proper functions based on those
        /// arguments. If the proper arguments are not properly set, then print a usage statement and exit the code.
        /// </summary>
        /// <param name="args">The list of arguments.</param>
        private static void Main(string[] args)
        {
            // Check the number of arguments
            var argsLength = args.Length;
            // Check to make sure there are a correct number of command line arguments. Print the usage statement and 
            //      exit the code if there are not exactly 3
            if (argsLength != 3)
            {
                UsageStatement();
                Environment.Exit(0);
            }

            SequentialCounter sequential;
            ParallelCounter parallel;

            // Create a switch on the arguments to call the right function. It can either call parallel mode, sequential
            //      mode, or both.
            switch (args[1])
            {
                case "-s":
                    Console.WriteLine("{0}\n", args[2]);

                    sequential = new SequentialCounter(args[2]);
                    sequential.Run();
                    break;
                case "-p":
                    Console.WriteLine("{0}\n", args[2]);

                    parallel = new ParallelCounter(args[2]);
                    parallel.Run();
                    break;
                case "-b":
                    Console.WriteLine("{0}\n", args[2]);

                    parallel = new ParallelCounter(args[2]);
                    parallel.Run();

                    Console.WriteLine("");

                    sequential = new SequentialCounter(args[2]);
                    sequential.Run();
                    break;
                default:
                    UsageStatement();
                    Environment.Exit(0);
                    break;
            }
        }

        /// <summary>
        /// This function will print the usage statement to the console if the command line arguments have not
        /// been configured properly.
        /// </summary>
        private static void UsageStatement()
        {
            Console.WriteLine("Usage: du [-s] [-p] [-b] <path>");
            Console.WriteLine("Summarize disk usage of the set of FILES, recursively for directories.");
            Console.WriteLine("You MUST specify one of the parameters, -s, -p, or -b");
            Console.WriteLine("-s       Run in single threaded mode");
            Console.WriteLine("-p       Run in parallel mode (uses all available processors)");
            Console.WriteLine("-b       Run in both parallel and single threaded mode.");
            Console.WriteLine("         Runs parallel followed by sequential mode");
        }
    }
    
    /// <summary>
    /// The SequentialCounter class is the class that will run the du command in sequential mode. It will count the 
    /// number of folders, files, and total bytes of a recursive directory listing by using a regular foreach loop. The 
    /// count will be printed to the console with proper formatting.
    /// </summary>
    public class SequentialCounter
    {
        private readonly string _directory;
        private int FolderCount { get; set; }
        private int FileCount { get; set; }
        private long ByteCount { get; set; }

        public SequentialCounter(string directory)
        {
            _directory = directory;
            FolderCount = 0;
            FileCount = 0;
            ByteCount = 0;
        }

        /// <summary>
        /// This method will be the recursive function to list through the directories and lists and update the counts.
        /// This will be done using standard foreach loops. Also, Unauthorized exceptions will be handled.
        /// </summary>
        /// <param name="directories">The string containing the current directory</param>
        private void CountFilesAndDirectories(string directories)
        {
            try
            {
                foreach (var d in Directory.GetDirectories(directories))
                {
                    try
                    {
                        foreach (var f in Directory.GetFiles(d))
                        {
                            FileCount++;
                            var fileInfo = new FileInfo(f);
                            fileInfo.Refresh();
                            ByteCount += fileInfo.Length;
                        }

                        FolderCount++;
                        CountFilesAndDirectories(d);
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>
        /// The Run function will find the total time it took the program to run and print that along with the counts
        /// for the files, folders, and total bytes.
        /// </summary>
        public void Run()
        {
            // This stopwatch will tell us how long the program ran for
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            
            CountFilesAndDirectories(_directory);
            
            stopWatch.Stop();
            // Print the time it took for the program to run in seconds
            Console.WriteLine("Sequential Calculated in: {0}s", stopWatch.Elapsed.TotalSeconds);
            Console.WriteLine("{0:n0} folders, {1:n0} files, {2:n0} bytes", FolderCount, FileCount, ByteCount);
        }
    }

    /// <summary>
    /// The ParallelCounter class is the class that will run the du command in parallel mode. It will count the number
    /// of folders, files, and total bytes of a recursive directory listing by using a parallel foreach loop. The count
    /// will be printed to the console with proper formatting.
    /// </summary>
    public class ParallelCounter
    {
        private readonly string _directory;
        private int _folderCount = 0;
        private int _fileCount = 0;
        private long _byteCount = 0;
        
        public ParallelCounter(string directory)
        {
            _directory = directory;
        }

        /// <summary>
        /// This method will be the recursive function to list through the directories and lists and update the counts.
        /// This will be done using parallel foreach loops. The counts have been updated from the sequential class to be
        /// able to use reference variables, since that will be required by interlocks. The interlock will protect the
        /// numbers being updated while executing the threads. Also, Unauthorized exceptions will be handled.
        /// </summary>
        /// <param name="directories">The string containing the current directory</param>
        private void CountFilesAndDirectories(string directories)
        {
            try
            {
                Parallel.ForEach(Directory.GetDirectories(directories), d =>
                {
                    try
                    {
                        Parallel.ForEach(Directory.GetFiles(d), f =>
                        {
                            Interlocked.Increment(ref _fileCount);
                            var fileInfo = new FileInfo(f);
                            fileInfo.Refresh();
                            Interlocked.Add(ref _byteCount, fileInfo.Length);
                        });

                        Interlocked.Increment(ref _folderCount);
                        CountFilesAndDirectories(d);
                    }
                    catch (UnauthorizedAccessException) { }
                });
            }
            catch (UnauthorizedAccessException) { }
        }
        
        /// <summary>
        /// The Run function will find the total time it took the program to run and print that along with the counts
        /// for the files, folders, and total bytes.
        /// </summary>
        public void Run()
        {
            // This stopwatch will tell us how long the program ran for
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            CountFilesAndDirectories(_directory);

            stopWatch.Stop();
            // Print the time it took for the program to run in seconds
            Console.WriteLine("Parallel Calculated in: {0}s", stopWatch.Elapsed.TotalSeconds);
            Console.WriteLine("{0:n0} folders, {1:n0} files, {2:n0} bytes", _folderCount, _fileCount, _byteCount);
        }
    }
}