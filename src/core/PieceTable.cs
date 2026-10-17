using System;
using System.Collections.Generic;
using System.Text;

namespace ZenQOffice.Core
{
    public enum BufferType
    {
        Original,
        Add
    }

    public class Piece
    {
        public BufferType BufferType { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }

        public Piece(BufferType type, int start, int length)
        {
            BufferType = type;
            Start = start;
            Length = length;
        }
    }

    public class PieceTable
    {
        private StringBuilder originalBuffer = new StringBuilder();
        private StringBuilder addBuffer = new StringBuilder();
        private List<Piece> pieces = new List<Piece>();
        private string _cachedText = null;
        private int _cachedTextVersion = -1;

        public int Version { get; private set; }

        public PieceTable()
        {
            Clear();
        }

        public PieceTable(string initialText)
        {
            SetInitialText(initialText);
        }

        public void Clear()
        {
            originalBuffer.Clear();
            addBuffer.Clear();
            pieces.Clear();
            _cachedText = null;
            Version++;
        }

        public void SetInitialText(string text)
        {
            originalBuffer.Clear();
            originalBuffer.Append(text ?? "");
            addBuffer.Clear();
            pieces.Clear();
            if (originalBuffer.Length > 0)
            {
                pieces.Add(new Piece(BufferType.Original, 0, originalBuffer.Length));
            }
            _cachedText = null;
            Version++;
        }

        public int Length
        {
            get
            {
                int total = 0;
                foreach (var piece in pieces) total += piece.Length;
                return total;
            }
        }

        private Tuple<int, int> FindPieceIndex(int offset)
        {
            int accumulated = 0;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (offset < accumulated + pieces[i].Length)
                {
                    return new Tuple<int, int>(i, offset - accumulated);
                }
                accumulated += pieces[i].Length;
            }
            // If offset is exactly at the end of the text (offset == Length)
            if (offset == accumulated && pieces.Count > 0)
            {
                return new Tuple<int, int>(pieces.Count - 1, pieces[pieces.Count - 1].Length);
            }
            return new Tuple<int, int>(pieces.Count, 0);
        }

        public void Insert(int index, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            int textLen = text.Length;
            int addOffset = addBuffer.Length;
            addBuffer.Append(text);

            if (pieces.Count == 0)
            {
                pieces.Add(new Piece(BufferType.Add, addOffset, textLen));
                _cachedText = null;
                Version++;
                return;
            }

            index = Math.Min(index, Length);
            var location = FindPieceIndex(index);
            int pieceIdx = location.Item1;
            int pieceOffset = location.Item2;

            if (pieceIdx >= pieces.Count)
            {
                pieces.Add(new Piece(BufferType.Add, addOffset, textLen));
                _cachedText = null;
                Version++;
                return;
            }

            Piece targetPiece = pieces[pieceIdx];

            if (pieceOffset == 0)
            {
                pieces.Insert(pieceIdx, new Piece(BufferType.Add, addOffset, textLen));
            }
            else if (pieceOffset == targetPiece.Length)
            {
                pieces.Insert(pieceIdx + 1, new Piece(BufferType.Add, addOffset, textLen));
            }
            else
            {
                Piece leftPiece = new Piece(targetPiece.BufferType, targetPiece.Start, pieceOffset);
                Piece rightPiece = new Piece(targetPiece.BufferType, targetPiece.Start + pieceOffset, targetPiece.Length - pieceOffset);

                pieces[pieceIdx] = leftPiece;
                pieces.Insert(pieceIdx + 1, new Piece(BufferType.Add, addOffset, textLen));
                pieces.Insert(pieceIdx + 2, rightPiece);
            }

            _cachedText = null;
            Version++;
        }

        public void Remove(int index, int length)
        {
            if (length <= 0 || pieces.Count == 0) return;

            int totalLen = Length;
            if (index >= totalLen) return;
            length = Math.Min(length, totalLen - index);

            int remainingToRemove = length;
            while (remainingToRemove > 0 && index < Length)
            {
                var location = FindPieceIndex(index);
                int pieceIdx = location.Item1;
                int pieceOffset = location.Item2;

                if (pieceIdx >= pieces.Count) break;

                Piece p = pieces[pieceIdx];
                int availableInPiece = p.Length - pieceOffset;
                int toRemoveHere = Math.Min(remainingToRemove, availableInPiece);

                if (pieceOffset == 0 && toRemoveHere == p.Length)
                {
                    pieces.RemoveAt(pieceIdx);
                }
                else if (pieceOffset == 0)
                {
                    p.Start += toRemoveHere;
                    p.Length -= toRemoveHere;
                }
                else if (pieceOffset + toRemoveHere == p.Length)
                {
                    p.Length -= toRemoveHere;
                }
                else
                {
                    Piece rightPiece = new Piece(p.BufferType, p.Start + pieceOffset + toRemoveHere, p.Length - (pieceOffset + toRemoveHere));
                    p.Length = pieceOffset;
                    pieces.Insert(pieceIdx + 1, rightPiece);
                }

                remainingToRemove -= toRemoveHere;
            }

            _cachedText = null;
            Version++;
        }

        public string GetText()
        {
            if (_cachedText != null && _cachedTextVersion == Version)
            {
                return _cachedText;
            }
            StringBuilder sb = new StringBuilder(Length);
            foreach (var piece in pieces)
            {
                StringBuilder src = (piece.BufferType == BufferType.Original) ? originalBuffer : addBuffer;
                sb.Append(src.ToString(piece.Start, piece.Length));
            }
            _cachedText = sb.ToString();
            _cachedTextVersion = Version;
            return _cachedText;
        }

        public string GetSubstring(int start, int length)
        {
            if (length <= 0 || start < 0 || start >= Length) return "";
            string full = GetText();
            if (start + length > full.Length) length = full.Length - start;
            return full.Substring(start, length);
        }
    }
}
