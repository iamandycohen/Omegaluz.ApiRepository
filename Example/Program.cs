using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {

            var api = new NetflixApiRepository();
            var titlesTask = api.GetTitlesAsync();
            titlesTask.Wait();

            string titles = titlesTask.Result;
            Console.WriteLine(titles);
            Console.ReadKey();

        }

    }
}
