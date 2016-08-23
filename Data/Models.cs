using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PsqlAdapter.Data
{
    public class laddercharacter
    {
        public string charname { get; set; }
        public bool charcore { get; set; }
        public bool charclassic { get; set; }
        public string charrealm { get; set; }
    }
    
    public class wsbuser
    {
        [Key]
        public int userid { get; set; }
        public string username { get; set; }
        public string usermodes { get; set; }
        public string userchannel { get; set; }
        public int userauth { get; set; }
        public DateTime userseen { get; set; }

        public ICollection<wsbticker> Tickers { get; set; }
    }

    public class wsbticker
    {
        [Key]
        public int tickerid { get; set; }
        public string tickername { get; set; }
        public int tickercost { get; set; }
        public DateTime tickerdate { get; set; }


        public wsbuser wsbuser { get; set; }
        [ForeignKey("userid")]
        public int userid { get; set; }
    }

}
