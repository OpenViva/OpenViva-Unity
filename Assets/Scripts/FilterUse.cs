namespace viva
{


    public class FilterUse
    {

        public Character owner { get { return queue.Count > 0 ? queue.objects[0] : null; } }

        private Set<Character> queue = new Set<Character>();


        public void SetOwner(Character character)
        {
            if (character == null)
            {
                return;
            }
            queue.Add(character);
        }

        public void RemoveOwner(Character character)
        {
            if (character == null)
            {
                return;
            }
            queue.Remove(character);
        }

        public int GetQueueIndex(Character character)
        {
            return queue.objects.IndexOf(character);
        }
    }

}