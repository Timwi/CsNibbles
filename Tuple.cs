/*
 * 
 *      The authors waive all rights to this source file.
 *      It is public domain where permitted by law.
 * 
 */

namespace Nibbles.Bas
{
    public struct Tuple<T1, T2>
    {
        private T1 _item1;
        private T2 _item2;
        public T1 Item1 { get { return _item1; } }
        public T2 Item2 { get { return _item2; } }
        public Tuple(T1 item1, T2 item2)
        {
            _item1 = item1;
            _item2 = item2;
        }
    }
}