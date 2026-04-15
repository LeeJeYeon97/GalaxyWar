// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("MXGme+dL2M/SmKDVhhgjzngqYyNBRXfbaWZSSnFmcYj9/kRzhjkY9IXM8Di9PSmb6xX78fjmWFMEcz9/vbjX2ChTsppsevY0pAOchp5hePMGt9GoT4cUq2ZdOuQJsgWJo8LzCifRf2+WJeHlqx+6KZi08ramnwQyDebPRUKazJfho3PaeQ39EjGCYZ6RI6CDkaynqIsn6SdWrKCgoKShoiOgrqGRI6CroyOgoKE0sR1Seke+e+08Fws7Bz00pNTYnncJue3znRbYIEW5pQtWj1e8BC0/6AjiU/cMBhcIT1iWoPknuwnhQklo4bqZh2ipDWaMOM/bZOdU0Z57pyz9cPQRerAUYPZZWgjlA+4rMRr/Ufepal4D8jxtUmhCXJcd7KOioKGg");
        private static int[] order = new int[] { 13,7,3,5,4,11,10,13,13,11,13,13,13,13,14 };
        private static int key = 161;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
