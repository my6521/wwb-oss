using System;

namespace WWB.OSS.Models
{
    public class Bucket
    {
        public string Location { get; set; }
        public string Name { get; set; }
        public Owner Owner { get; set; }

        private DateTime _creationDate = DateTime.MinValue;

        public string CreationDate
        {
            get
            {
                return _creationDate.ToString("yyyy-MM-dd HH:mm:ss");
            }
            set
            {
                if (DateTime.TryParse(value, out DateTime dt))
                {
                    _creationDate = dt;
                }
                _creationDate = DateTime.MinValue;
            }
        }
    }
}