namespace NMX.ShaolinSudoku.Library.Core
{
    using System;
    using System.Collections.Generic;

    public class SudokuOperations
    {
        int[] solArr = new int[81];
        int[,] number0 = new int[9, 9];
        int[,] numberS = new int[9, 9];
        int[,] numLineNcheckSol = new int[9, 9];
        List<int> digits = new List<int>(9);
        List<int> shufflePos = new List<int>(3);
        List<int> removePos = new List<int>(81);
        private readonly Random random = new Random();

        public int[] GetSudoku(int r)
        {
            FillDiagonalBoxes();
            RandomizeNumberLine();
            FillRemaining(0, true);
            Shuffle();
            RemoveSomeNumbers(r);
            for (int i = 0; i < 81; i++)
            {
                solArr[i] = numberS[i / 9, i % 9] * 10 + number0[i / 9, i % 9];
            }
            return solArr;
        }

        public int[] SolveSudoku(int[] puzz)
        {
            for (int i = 0; i < 81; i++)
            {
                numberS[i / 9, i % 9] = number0[i / 9, i % 9] = puzz[i];
            }
            int n;
            for (int i = 0; i < 81; i++)
            {
                n = numberS[i / 9, i % 9];
                if (n != 0)
                {
                    numberS[i / 9, i % 9] = 0;
                    if (!UnusedInRowCol(i / 9, i % 9, n, false) || !UnusedInBox((i / 9) / 3, (i % 9) / 3, n, false))
                    {
                        return null;
                    }
                    numberS[i / 9, i % 9] = n;
                }
            }
            FillRemaining(0, false);
            for (int i = 0; i < 81; i++)
            {
                if (numberS[i / 9, i % 9] == 0)
                {
                    return null;
                }
                solArr[i] = numberS[i / 9, i % 9];
            }
            if (!CheckUniqueSolution(false))
            {
                return null;
            }
            return solArr;
        }

        void ResetDigits()
        {
            digits.Clear();
            for (int i = 1; i < 10; i++)
            {
                digits.Add(i);
            }
        }

        void ResetIndexPos()
        {
            shufflePos.Clear();
            for (int i = 0; i < 3; i++)
            {
                shufflePos.Add(i);
            }
        }

        void ResetRemovePos()
        {
            removePos.Clear();
            for (int i = 0; i < 81; i++)
            {
                removePos.Add(i);
            }
        }

        void ResetNumCheckSol()
        {
            for (int i = 0; i < 81; i++)
            {
                numLineNcheckSol[i / 9, i % 9] = number0[i / 9, i % 9];
            }
        }

        void RandomizeNumberLine()
        {
            int k;
            ResetDigits();
            for (int i = 0; i < 81; i++)
            {
                k = random.Next(0, digits.Count);
                numLineNcheckSol[i / 9, i % 9] = digits[k];
                digits.RemoveAt(k);
                if (digits.Count == 0)
                {
                    ResetDigits();
                }
            }
        }

        void Shuffle()
        {
            int k1, k2, t;
            for (int a = 0; a < 3; a++)
            {
                ResetIndexPos();
                k1 = shufflePos[random.Next(0, shufflePos.Count)];
                shufflePos.Remove(k1);
                k2 = shufflePos[random.Next(0, shufflePos.Count)];
                shufflePos.Remove(k2);
                for (int j = 0; j < 9; j++)
                {
                    t = numberS[shufflePos[0] + (a * 3), j];
                    numberS[shufflePos[0] + (a * 3), j] = numberS[k1 + (a * 3), j];
                    numberS[k1 + (a * 3), j] = numberS[k2 + (a * 3), j];
                    numberS[k2 + (a * 3), j] = t;
                }
            }
            for (int a = 0; a < 3; a++)
            {
                ResetIndexPos();
                k1 = shufflePos[random.Next(0, shufflePos.Count)];
                shufflePos.Remove(k1);
                k2 = shufflePos[random.Next(0, shufflePos.Count)];
                shufflePos.Remove(k2);
                for (int i = 0; i < 9; i++)
                {
                    t = numberS[i, shufflePos[0] + (a * 3)];
                    numberS[i, shufflePos[0] + (a * 3)] = numberS[i, k1 + (a * 3)];
                    numberS[i, k1 + (a * 3)] = numberS[i, k2 + (a * 3)];
                    numberS[i, k2 + (a * 3)] = t;
                }
            }
            ResetIndexPos();
            k1 = shufflePos[random.Next(0, shufflePos.Count)];
            shufflePos.Remove(k1);
            k2 = shufflePos[random.Next(0, shufflePos.Count)];
            shufflePos.Remove(k2);
            for (int i = 0; i < 27; i++)
            {
                t = numberS[(i / 9) + (shufflePos[0] * 3), i % 9];
                numberS[(i / 9) + (shufflePos[0] * 3), i % 9] = numberS[(i / 9) + (k1 * 3), i % 9];
                numberS[(i / 9) + (k1 * 3), i % 9] = numberS[(i / 9) + (k2 * 3), i % 9];
                numberS[(i / 9) + (k2 * 3), i % 9] = t;
            }
            ResetIndexPos();
            k1 = shufflePos[random.Next(0, shufflePos.Count)];
            shufflePos.Remove(k1);
            k2 = shufflePos[random.Next(0, shufflePos.Count)];
            shufflePos.Remove(k2);
            for (int i = 0; i < 27; i++)
            {
                t = numberS[(i % 9), i / 9 + (shufflePos[0] * 3)];
                numberS[(i % 9), i / 9 + (shufflePos[0] * 3)] = numberS[(i % 9), i / 9 + (k1 * 3)];
                numberS[(i % 9), i / 9 + (k1 * 3)] = numberS[(i % 9), i / 9 + (k2 * 3)];
                numberS[(i % 9), i / 9 + (k2 * 3)] = t;
            }
        }

        void FillDiagonalBoxes()
        {
            int k;
            ResetDigits();
            for (int i = 0; i < 27; i++)
            {
                k = random.Next(0, digits.Count);
                numberS[i / 3, ((i / 9) * 3) + (i % 3)] = digits[k];
                digits.RemoveAt(k);
                if (digits.Count == 0)
                {
                    ResetDigits();
                }
            }
        }

        bool UnusedInRowCol(int ii, int jj, int n, bool ch)
        {
            if (ch)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (numLineNcheckSol[ii, j] == n)
                    {
                        return false;
                    }
                }
                for (int i = 0; i < 9; i++)
                {
                    if (numLineNcheckSol[i, jj] == n)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                for (int j = 0; j < 9; j++)
                {
                    if (numberS[ii, j] == n)
                    {
                        return false;
                    }
                }
                for (int i = 0; i < 9; i++)
                {
                    if (numberS[i, jj] == n)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        bool UnusedInBox(int a, int b, int n, bool ch)
        {
            if (ch)
            {
                for (int i = 0; i < 9; i++)
                {
                    if (numLineNcheckSol[(i / 3) + (a * 3), (i % 3) + (b * 3)] == n)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                for (int i = 0; i < 9; i++)
                {
                    if (numberS[(i / 3) + (a * 3), (i % 3) + (b * 3)] == n)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        bool FillRemaining(int i, bool rn)
        {
            if (i > 80)
            {
                return true;
            }
            while (numberS[i / 9, i % 9] != 0)
            {
                i++;
                if (i > 80)
                {
                    return true;
                }
            }
            int n;
            for (int a = 0; a < 9; a++)
            {
                if (rn)
                {
                    n = numLineNcheckSol[i / 9, a];
                }
                else
                {
                    n = a + 1;
                }
                if (UnusedInRowCol(i / 9, i % 9, n, false) && UnusedInBox((i / 9) / 3, (i % 9) / 3, n, false))
                {
                    numberS[i / 9, i % 9] = n;

                    if (FillRemaining(i + 1, rn))
                    {
                        return true;
                    }
                    numberS[i / 9, i % 9] = 0;
                }
            }
            return false;
        }

        void RemoveSomeNumbers(int r)
        {
            int k, t;
            int g = r;
            ResetRemovePos();
            for (int i = 0; i < 81; i++)
            {
                number0[i / 9, i % 9] = numberS[i / 9, i % 9];
            }
            for (int i = 0; i < g; i++)
            {
                k = random.Next(0, removePos.Count);
                t = number0[removePos[k] / 9, removePos[k] % 9];
                number0[removePos[k] / 9, removePos[k] % 9] = 0;

                if (!CheckUniqueSolution(true))
                {
                    number0[removePos[k] / 9, removePos[k] % 9] = t;
                    g++;
                }
                removePos.RemoveAt(k);
                if (removePos.Count == 0)
                {
                    return;
                }
            }
        }

        bool SolveCheckSolA(int i, bool rn)
        {
            if (i > 80)
            {
                return true;
            }
            while (numLineNcheckSol[i / 9, i % 9] != 0)
            {
                i++;
                if (i > 80)
                {
                    return true;
                }
            }
            int n;
            for (int a = 0; a < 9; a++)
            {

                if (rn)
                {
                    n = numberS[i / 9, i % 9] + a + 1;
                    if (n > 9)
                    {
                        n -= 9;
                    }
                }
                else
                {
                    n = 9 - a;
                }
                if (UnusedInRowCol(i / 9, i % 9, n, true) && UnusedInBox((i / 9) / 3, (i % 9) / 3, n, true))
                {
                    numLineNcheckSol[i / 9, i % 9] = n;

                    if (SolveCheckSolA(i + 1, rn))
                    {
                        return true;
                    }
                    numLineNcheckSol[i / 9, i % 9] = 0;
                }
            }
            return false;
        }

        bool CheckUniqueSolution(bool rn)
        {
            ResetNumCheckSol();
            SolveCheckSolA(0, rn);
            for (int i = 0; i < 81; i++)
            {
                if (number0[i / 9, i % 9] == 0 && numLineNcheckSol[i / 9, i % 9] != numberS[i / 9, i % 9])
                {
                    return false;
                }
            }
            return true;
        }

    }
}
