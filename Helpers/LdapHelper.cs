using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodigosQRComprasCEDIS_2._0.Helpers
{
    public class LdapHelper
    {
        public static LdapConnection GetConnection()
        {
            LdapConnection ldap = new LdapConnection() { SecureSocketLayer = false };
            String ldapHost = "187.141.228.93";
            //String ldapHost = "DC01";
            //Connect function will create a socket connection to the server - Port 389 for insecure and 3269 for secure
            ldap.Connect(ldapHost,389);
            ldap.Bind("sistemas12@atp.local", "Avance3");
            return ldap;
        }

        public List<EntryAD> SearchForGroup(string groupName)
        {
            var ldapConn = GetConnection();
            Dictionary<String, String[]> groups = new Dictionary<string, String[]>();

            var searchBase = "DC=atp,DC=local";
            var filter = "(sAMAccountName=sistemas09)";
            var search = ldapConn.Search(searchBase, LdapConnection.ScopeSub, filter, null, false);
            List<EntryAD> entryList = new List<EntryAD>();
            while (search.HasMore())
            {
                try
                {
                    var nextEntry = search.Next();
                    foreach(var entry in nextEntry.GetAttributeSet().Values)
                    {
                        entryList.Add(new EntryAD() { Key = entry.Name, Values= entry.StringValueArray });
                    }
                }catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return entryList;
        }

        public class EntryAD
        {
            public String Key { get; set; }
            public String[] Values { get; set; }
        }
    }
}
