using System.Collections.Generic;

namespace viva
{

    public class FirstServeItemCallStack
    {

        public delegate bool ServeItemCallback(Item item);

        private List<ServeItemCallback> stack = new List<ServeItemCallback>();


        public void AddCallback(ServeItemCallback func)
        {
            if (stack.Contains(func) || func == null)
            {
                return;
            }
            stack.Add(func);
        }

        public void RemoveCallback(ServeItemCallback func)
        {
            //stack.Add( func );	// <------------------------------------------------------------------- Copy&Paste error? -----------------------------------<<<
            if (stack.Contains(func))
            {
                stack.Remove(func);
            }
        }

        public void Call(Item item)
        {
            if (item == null)
            {
                return;
            }
            int index = stack.Count - 1;
            //while( index > 0 && index < stack.Count ){	// <-------------------- wrong stack index. also, can index<stack.Count ever happen?-------<<<
            while (index >= 0)
            {
                var func = stack[index--];
                if (func.Invoke(item))
                {
                    break;
                }
                // I guess index<stack.Count could only happen if func were to remove items from the stack.
                // If that is actually possible, then we must also allow for func to remove more than one item.
                // Reset index and keep working with whatever is left on stack.
                if (index >= stack.Count)
                {
                    index = stack.Count - 1;
                }
            }
        }
    }

}