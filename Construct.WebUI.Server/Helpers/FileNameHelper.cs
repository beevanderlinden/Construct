namespace Construct.WebUI.Server.Helpers
{
    public static class FileNameHelper
    {
        /// <summary>
        /// Genereert een unieke bestandsnaam door suffixen toe te voegen zoals (1), (2), etc.
        /// </summary>
        /// <param name="fileName">Originele bestandsnaam inclusief extensie.</param>
        /// <param name="existingFileNames">Lijst van bestandsnamen die al bestaan.</param>
        /// <returns>Een unieke bestandsnaam.</returns>
        public static string GetUniqueFileName(string fileName, IEnumerable<string> existingFileNames)
        {
            var nameSet = new HashSet<string?>(existingFileNames);

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            if (!nameSet.Contains(fileName))
                return fileName;

            int counter = 1;
            string newName;
            do
            {
                newName = $"{nameWithoutExtension} ({counter}){extension}";
                counter++;
            } while (nameSet.Contains(newName));

            return newName;
        }
    }
}
