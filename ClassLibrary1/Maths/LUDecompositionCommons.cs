using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
namespace ClassLibrary1.Maths
{
    public class LUDecompositionCommons
    {
        public int rows;
        public int cols;
        public double[,] mat;

        public LUDecompositionCommons L;
        public LUDecompositionCommons U;
        private int[] pi;
        private double detOfP = 1;
        public LUDecompositionCommons()         // LUDecompositionCommons Class constructor
        {

        }
        public LUDecompositionCommons(int iRows, int iCols)         // LUDecompositionCommons Class constructor
        {
            rows = iRows;
            cols = iCols;
            mat = new double[rows, cols];
        }

        public Boolean IsSquare()
        {
            return (rows == cols);
        }

        public double this[int iRow, int iCol]      // Access this LUDecompositionCommons as a 2D array
        {
            get { return mat[iRow, iCol]; }
            set { mat[iRow, iCol] = value; }
        }

        public LUDecompositionCommons GetCol(int k)
        {
            LUDecompositionCommons m = new LUDecompositionCommons(rows, 1);
            for (int i = 0; i < rows; i++) m[i, 0] = mat[i, k];
            return m;
        }

        public void SetCol(LUDecompositionCommons v, int k)
        {
            for (int i = 0; i < rows; i++) mat[i, k] = v[i, 0];
        }

        public void MakeLU()                        // Function for LU decomposition
        {
            if (!IsSquare()) throw new MException("The LUDecompositionCommons is not square!");
            L = IdentityLUDecompositionCommons(rows, cols);
            U = Duplicate();

            pi = new int[rows];
            for (int i = 0; i < rows; i++) pi[i] = i;

            double p = 0;
            double pom2;
            int k0 = 0;
            int pom1 = 0;

            for (int k = 0; k < cols - 1; k++)
            {
                p = 0;
                for (int i = k; i < rows; i++)      // find the row with the biggest pivot
                {
                    if (Math.Abs(U[i, k]) > p)
                    {
                        p = Math.Abs(U[i, k]);
                        k0 = i;
                    }
                }
                if (p == 0) // samé nuly ve sloupci
                    throw new MException("The LUDecompositionCommons is singular!");

                pom1 = pi[k]; pi[k] = pi[k0]; pi[k0] = pom1;    // switch two rows in permutation LUDecompositionCommons

                for (int i = 0; i < k; i++)
                {
                    pom2 = L[k, i]; L[k, i] = L[k0, i]; L[k0, i] = pom2;
                }

                if (k != k0) detOfP *= -1;

                for (int i = 0; i < cols; i++)                  // Switch rows in U
                {
                    pom2 = U[k, i]; U[k, i] = U[k0, i]; U[k0, i] = pom2;
                }

                for (int i = k + 1; i < rows; i++)
                {
                    L[i, k] = U[i, k] / U[k, k];
                    for (int j = k; j < cols; j++)
                        U[i, j] = U[i, j] - L[i, k] * U[k, j];
                }
            }
        }


        public LUDecompositionCommons SolveWith(LUDecompositionCommons v)                        // Function solves Ax = v in confirmity with solution vector "v"
        {
            if (rows != cols) throw new MException("The LUDecompositionCommons is not square!");
            if (rows != v.rows) throw new MException("Wrong number of results in solution vector!");
            if (L == null) MakeLU();

            LUDecompositionCommons b = new LUDecompositionCommons(rows, 1);
            for (int i = 0; i < rows; i++) b[i, 0] = v[pi[i], 0];   // switch two items in "v" due to permutation LUDecompositionCommons

            LUDecompositionCommons z = SubsForth(L, b);
            LUDecompositionCommons x = SubsBack(U, z);

            return x;
        }

        public LUDecompositionCommons Invert()                                   // Function returns the inverted LUDecompositionCommons
        {
            if (L == null) MakeLU();

            LUDecompositionCommons inv = new LUDecompositionCommons(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                LUDecompositionCommons Ei = LUDecompositionCommons.ZeroLUDecompositionCommons(rows, 1);
                Ei[i, 0] = 1;
                LUDecompositionCommons col = SolveWith(Ei);
                inv.SetCol(col, i);
            }
            return inv;
        }


        public double Det()                         // Function for determinant
        {
            if (L == null) MakeLU();
            double det = detOfP;
            for (int i = 0; i < rows; i++) det *= U[i, i];
            return det;
        }

        public LUDecompositionCommons GetP()                        // Function returns permutation LUDecompositionCommons "P" due to permutation vector "pi"
        {
            if (L == null) MakeLU();

            LUDecompositionCommons LUDecompositionCommons = ZeroLUDecompositionCommons(rows, cols);
            for (int i = 0; i < rows; i++) LUDecompositionCommons[pi[i], i] = 1;
            return LUDecompositionCommons;
        }

        public LUDecompositionCommons Duplicate()                   // Function returns the copy of this LUDecompositionCommons
        {
            LUDecompositionCommons LUDecompositionCommons = new LUDecompositionCommons(rows, cols);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    LUDecompositionCommons[i, j] = mat[i, j];
            return LUDecompositionCommons;
        }

        public static LUDecompositionCommons SubsForth(LUDecompositionCommons A, LUDecompositionCommons b)          // Function solves Ax = b for A as a lower triangular LUDecompositionCommons
        {
            if (A.L == null) A.MakeLU();
            int n = A.rows;
            LUDecompositionCommons x = new LUDecompositionCommons(n, 1);

            for (int i = 0; i < n; i++)
            {
                x[i, 0] = b[i, 0];
                for (int j = 0; j < i; j++) x[i, 0] -= A[i, j] * x[j, 0];
                x[i, 0] = x[i, 0] / A[i, i];
            }
            return x;
        }

        public static LUDecompositionCommons SubsBack(LUDecompositionCommons A, LUDecompositionCommons b)           // Function solves Ax = b for A as an upper triangular LUDecompositionCommons
        {
            if (A.L == null) A.MakeLU();
            int n = A.rows;
            LUDecompositionCommons x = new LUDecompositionCommons(n, 1);

            for (int i = n - 1; i > -1; i--)
            {
                x[i, 0] = b[i, 0];
                for (int j = n - 1; j > i; j--) x[i, 0] -= A[i, j] * x[j, 0];
                x[i, 0] = x[i, 0] / A[i, i];
            }
            return x;
        }

        public static LUDecompositionCommons ZeroLUDecompositionCommons(int iRows, int iCols)       // Function generates the zero LUDecompositionCommons
        {
            LUDecompositionCommons matrix = new LUDecompositionCommons(iRows, iCols);
            for (int i = 0; i < iRows; i++)
                for (int j = 0; j < iCols; j++)
                    matrix[i, j] = 0;
            return matrix;
        }

        public static LUDecompositionCommons IdentityLUDecompositionCommons(int iRows, int iCols)   // Function generates the identity LUDecompositionCommons
        {
            LUDecompositionCommons LUDecompositionCommons = ZeroLUDecompositionCommons(iRows, iCols);
            for (int i = 0; i < Math.Min(iRows, iCols); i++)
                LUDecompositionCommons[i, i] = 1;
            return LUDecompositionCommons;
        }

        public static LUDecompositionCommons RandomLUDecompositionCommons(int iRows, int iCols, int dispersion)       // Function generates the random LUDecompositionCommons
        {
            Random random = new Random();
            LUDecompositionCommons LUDecompositionCommons = new LUDecompositionCommons(iRows, iCols);
            for (int i = 0; i < iRows; i++)
                for (int j = 0; j < iCols; j++)
                    LUDecompositionCommons[i, j] = random.Next(-dispersion, dispersion);
            return LUDecompositionCommons;
        }

        public static LUDecompositionCommons Parse(string ps)                        // Function parses the LUDecompositionCommons from string
        {
            string s = NormalizeLUDecompositionCommonsString(ps);
            string[] rows = Regex.Split(s, "\r\n");
            string[] nums = rows[0].Split(' ');
            LUDecompositionCommons LUDecompositionCommons = new LUDecompositionCommons(rows.Length, nums.Length);
            try
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    nums = rows[i].Split(' ');
                    for (int j = 0; j < nums.Length; j++) LUDecompositionCommons[i, j] = double.Parse(nums[j]);
                }
            }
            catch (FormatException exc) { throw new MException("Wrong input format!"); }
            return LUDecompositionCommons;
        }

        public override string ToString()                           // Function returns LUDecompositionCommons as a string
        {
            string s = "";
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++) s += String.Format("{0,5:0.00}", mat[i, j]) + " ";
                s += "\r\n";
            }
            return s;
        }

        public static LUDecompositionCommons Transpose(LUDecompositionCommons m)              // LUDecompositionCommons transpose, for any rectangular LUDecompositionCommons
        {
            LUDecompositionCommons t = new LUDecompositionCommons(m.cols, m.rows);
            for (int i = 0; i < m.rows; i++)
                for (int j = 0; j < m.cols; j++)
                    t[j, i] = m[i, j];
            return t;
        }

        public static LUDecompositionCommons Power(LUDecompositionCommons m, int pow)           // Power LUDecompositionCommons to exponent
        {
            if (pow == 0) return IdentityLUDecompositionCommons(m.rows, m.cols);
            if (pow == 1) return m.Duplicate();
            if (pow == -1) return m.Invert();

            LUDecompositionCommons x;
            if (pow < 0) { x = m.Invert(); pow *= -1; }
            else x = m.Duplicate();

            LUDecompositionCommons ret = IdentityLUDecompositionCommons(m.rows, m.cols);
            while (pow != 0)
            {
                if ((pow & 1) == 1) ret *= x;
                x *= x;
                pow >>= 1;
            }
            return ret;
        }

        private static void SafeAplusBintoC(LUDecompositionCommons A, int xa, int ya, LUDecompositionCommons B, int xb, int yb, LUDecompositionCommons C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++)     // cols
                {
                    C[i, j] = 0;
                    if (xa + j < A.cols && ya + i < A.rows) C[i, j] += A[ya + i, xa + j];
                    if (xb + j < B.cols && yb + i < B.rows) C[i, j] += B[yb + i, xb + j];
                }
        }

        private static void SafeAminusBintoC(LUDecompositionCommons A, int xa, int ya, LUDecompositionCommons B, int xb, int yb, LUDecompositionCommons C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++)     // cols
                {
                    C[i, j] = 0;
                    if (xa + j < A.cols && ya + i < A.rows) C[i, j] += A[ya + i, xa + j];
                    if (xb + j < B.cols && yb + i < B.rows) C[i, j] -= B[yb + i, xb + j];
                }
        }

        private static void SafeACopytoC(LUDecompositionCommons A, int xa, int ya, LUDecompositionCommons C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++)     // cols
                {
                    C[i, j] = 0;
                    if (xa + j < A.cols && ya + i < A.rows) C[i, j] += A[ya + i, xa + j];
                }
        }

        private static void AplusBintoC(LUDecompositionCommons A, int xa, int ya, LUDecompositionCommons B, int xb, int yb, LUDecompositionCommons C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++) C[i, j] = A[ya + i, xa + j] + B[yb + i, xb + j];
        }

        private static void AminusBintoC(LUDecompositionCommons A, int xa, int ya, LUDecompositionCommons B, int xb, int yb, LUDecompositionCommons C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++) C[i, j] = A[ya + i, xa + j] - B[yb + i, xb + j];
        }

        private static void ACopytoC(LUDecompositionCommons A, int xa, int ya, LUDecompositionCommons C, int size)
        {
            for (int i = 0; i < size; i++)          // rows
                for (int j = 0; j < size; j++) C[i, j] = A[ya + i, xa + j];
        }

        private static LUDecompositionCommons StrassenMultiply(LUDecompositionCommons A, LUDecompositionCommons B)                // Smart LUDecompositionCommons multiplication
        {
            if (A.cols != B.rows) throw new MException("Wrong dimension of LUDecompositionCommons!");

            LUDecompositionCommons R;

            int msize = Math.Max(Math.Max(A.rows, A.cols), Math.Max(B.rows, B.cols));

            if (msize < 32)
            {
                R = ZeroLUDecompositionCommons(A.rows, B.cols);
                for (int i = 0; i < R.rows; i++)
                    for (int j = 0; j < R.cols; j++)
                        for (int k = 0; k < A.cols; k++)
                            R[i, j] += A[i, k] * B[k, j];
                return R;
            }

            int size = 1; int n = 0;
            while (msize > size) { size *= 2; n++; };
            int h = size / 2;


            LUDecompositionCommons[,] mField = new LUDecompositionCommons[n, 9];

            /*
             *  8x8, 8x8, 8x8, ...
             *  4x4, 4x4, 4x4, ...
             *  2x2, 2x2, 2x2, ...
             *  . . .
             */

            int z;
            for (int i = 0; i < n - 4; i++)          // rows
            {
                z = (int)Math.Pow(2, n - i - 1);
                for (int j = 0; j < 9; j++) mField[i, j] = new LUDecompositionCommons(z, z);
            }

            SafeAplusBintoC(A, 0, 0, A, h, h, mField[0, 0], h);
            SafeAplusBintoC(B, 0, 0, B, h, h, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 1], 1, mField); // (A11 + A22) * (B11 + B22);

            SafeAplusBintoC(A, 0, h, A, h, h, mField[0, 0], h);
            SafeACopytoC(B, 0, 0, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 2], 1, mField); // (A21 + A22) * B11;

            SafeACopytoC(A, 0, 0, mField[0, 0], h);
            SafeAminusBintoC(B, h, 0, B, h, h, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 3], 1, mField); //A11 * (B12 - B22);

            SafeACopytoC(A, h, h, mField[0, 0], h);
            SafeAminusBintoC(B, 0, h, B, 0, 0, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 4], 1, mField); //A22 * (B21 - B11);

            SafeAplusBintoC(A, 0, 0, A, h, 0, mField[0, 0], h);
            SafeACopytoC(B, h, h, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 5], 1, mField); //(A11 + A12) * B22;

            SafeAminusBintoC(A, 0, h, A, 0, 0, mField[0, 0], h);
            SafeAplusBintoC(B, 0, 0, B, h, 0, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 6], 1, mField); //(A21 - A11) * (B11 + B12);

            SafeAminusBintoC(A, h, 0, A, h, h, mField[0, 0], h);
            SafeAplusBintoC(B, 0, h, B, h, h, mField[0, 1], h);
            StrassenMultiplyRun(mField[0, 0], mField[0, 1], mField[0, 1 + 7], 1, mField); // (A12 - A22) * (B21 + B22);

            R = new LUDecompositionCommons(A.rows, B.cols);                  // result

            /// C11
            for (int i = 0; i < Math.Min(h, R.rows); i++)          // rows
                for (int j = 0; j < Math.Min(h, R.cols); j++)     // cols
                    R[i, j] = mField[0, 1 + 1][i, j] + mField[0, 1 + 4][i, j] - mField[0, 1 + 5][i, j] + mField[0, 1 + 7][i, j];

            /// C12
            for (int i = 0; i < Math.Min(h, R.rows); i++)          // rows
                for (int j = h; j < Math.Min(2 * h, R.cols); j++)     // cols
                    R[i, j] = mField[0, 1 + 3][i, j - h] + mField[0, 1 + 5][i, j - h];

            /// C21
            for (int i = h; i < Math.Min(2 * h, R.rows); i++)          // rows
                for (int j = 0; j < Math.Min(h, R.cols); j++)     // cols
                    R[i, j] = mField[0, 1 + 2][i - h, j] + mField[0, 1 + 4][i - h, j];

            /// C22
            for (int i = h; i < Math.Min(2 * h, R.rows); i++)          // rows
                for (int j = h; j < Math.Min(2 * h, R.cols); j++)     // cols
                    R[i, j] = mField[0, 1 + 1][i - h, j - h] - mField[0, 1 + 2][i - h, j - h] + mField[0, 1 + 3][i - h, j - h] + mField[0, 1 + 6][i - h, j - h];

            return R;
        }

        // function for square LUDecompositionCommons 2^N x 2^N

        private static void StrassenMultiplyRun(LUDecompositionCommons A, LUDecompositionCommons B, LUDecompositionCommons C, int l, LUDecompositionCommons[,] f)    // A * B into C, level of recursion, LUDecompositionCommons field
        {
            int size = A.rows;
            int h = size / 2;

            if (size < 32)
            {
                for (int i = 0; i < C.rows; i++)
                    for (int j = 0; j < C.cols; j++)
                    {
                        C[i, j] = 0;
                        for (int k = 0; k < A.cols; k++) C[i, j] += A[i, k] * B[k, j];
                    }
                return;
            }

            AplusBintoC(A, 0, 0, A, h, h, f[l, 0], h);
            AplusBintoC(B, 0, 0, B, h, h, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 1], l + 1, f); // (A11 + A22) * (B11 + B22);

            AplusBintoC(A, 0, h, A, h, h, f[l, 0], h);
            ACopytoC(B, 0, 0, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 2], l + 1, f); // (A21 + A22) * B11;

            ACopytoC(A, 0, 0, f[l, 0], h);
            AminusBintoC(B, h, 0, B, h, h, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 3], l + 1, f); //A11 * (B12 - B22);

            ACopytoC(A, h, h, f[l, 0], h);
            AminusBintoC(B, 0, h, B, 0, 0, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 4], l + 1, f); //A22 * (B21 - B11);

            AplusBintoC(A, 0, 0, A, h, 0, f[l, 0], h);
            ACopytoC(B, h, h, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 5], l + 1, f); //(A11 + A12) * B22;

            AminusBintoC(A, 0, h, A, 0, 0, f[l, 0], h);
            AplusBintoC(B, 0, 0, B, h, 0, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 6], l + 1, f); //(A21 - A11) * (B11 + B12);

            AminusBintoC(A, h, 0, A, h, h, f[l, 0], h);
            AplusBintoC(B, 0, h, B, h, h, f[l, 1], h);
            StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 7], l + 1, f); // (A12 - A22) * (B21 + B22);

            /// C11
            for (int i = 0; i < h; i++)          // rows
                for (int j = 0; j < h; j++)     // cols
                    C[i, j] = f[l, 1 + 1][i, j] + f[l, 1 + 4][i, j] - f[l, 1 + 5][i, j] + f[l, 1 + 7][i, j];

            /// C12
            for (int i = 0; i < h; i++)          // rows
                for (int j = h; j < size; j++)     // cols
                    C[i, j] = f[l, 1 + 3][i, j - h] + f[l, 1 + 5][i, j - h];

            /// C21
            for (int i = h; i < size; i++)          // rows
                for (int j = 0; j < h; j++)     // cols
                    C[i, j] = f[l, 1 + 2][i - h, j] + f[l, 1 + 4][i - h, j];

            /// C22
            for (int i = h; i < size; i++)          // rows
                for (int j = h; j < size; j++)     // cols
                    C[i, j] = f[l, 1 + 1][i - h, j - h] - f[l, 1 + 2][i - h, j - h] + f[l, 1 + 3][i - h, j - h] + f[l, 1 + 6][i - h, j - h];
        }

        public static LUDecompositionCommons StupidMultiply(LUDecompositionCommons m1, LUDecompositionCommons m2)                  // Stupid LUDecompositionCommons multiplication
        {
            if (m1.cols != m2.rows) throw new MException("Wrong dimensions of LUDecompositionCommons!");

            LUDecompositionCommons result = ZeroLUDecompositionCommons(m1.rows, m2.cols);
            for (int i = 0; i < result.rows; i++)
                for (int j = 0; j < result.cols; j++)
                    for (int k = 0; k < m1.cols; k++)
                        result[i, j] += m1[i, k] * m2[k, j];
            return result;
        }
        private static LUDecompositionCommons Multiply(double n, LUDecompositionCommons m)                          // Multiplication by constant n
        {
            LUDecompositionCommons r = new LUDecompositionCommons(m.rows, m.cols);
            for (int i = 0; i < m.rows; i++)
                for (int j = 0; j < m.cols; j++)
                    r[i, j] = m[i, j] * n;
            return r;
        }
        private static LUDecompositionCommons Add(LUDecompositionCommons m1, LUDecompositionCommons m2)         // Sčítání matic
        {
            if (m1.rows != m2.rows || m1.cols != m2.cols) throw new MException("Matrices must have the same dimensions!");
            LUDecompositionCommons r = new LUDecompositionCommons(m1.rows, m1.cols);
            for (int i = 0; i < r.rows; i++)
                for (int j = 0; j < r.cols; j++)
                    r[i, j] = m1[i, j] + m2[i, j];
            return r;
        }

        public static string NormalizeLUDecompositionCommonsString(string matStr)   // From Andy - thank you! :)
        {
            // Remove any multiple spaces
            while (matStr.IndexOf("  ") != -1)
                matStr = matStr.Replace("  ", " ");

            // Remove any spaces before or after newlines
            matStr = matStr.Replace(" \r\n", "\r\n");
            matStr = matStr.Replace("\r\n ", "\r\n");

            // If the data ends in a newline, remove the trailing newline.
            // Make it easier by first replacing \r\n’s with |’s then
            // restore the |’s with \r\n’s
            matStr = matStr.Replace("\r\n", "|");
            while (matStr.LastIndexOf("|") == (matStr.Length - 1))
                matStr = matStr.Substring(0, matStr.Length - 1);

            matStr = matStr.Replace("|", "\r\n");
            return matStr.Trim();
        }

        //   O P E R A T O R S

        public static LUDecompositionCommons operator -(LUDecompositionCommons m)
        { return LUDecompositionCommons.Multiply(-1, m); }

        public static LUDecompositionCommons operator +(LUDecompositionCommons m1, LUDecompositionCommons m2)
        { return LUDecompositionCommons.Add(m1, m2); }

        public static LUDecompositionCommons operator -(LUDecompositionCommons m1, LUDecompositionCommons m2)
        { return LUDecompositionCommons.Add(m1, -m2); }

        public static LUDecompositionCommons operator *(LUDecompositionCommons m1, LUDecompositionCommons m2)
        { return LUDecompositionCommons.StrassenMultiply(m1, m2); }

        public static LUDecompositionCommons operator *(double n, LUDecompositionCommons m)
        { return LUDecompositionCommons.Multiply(n, m); }
    }
}
public class MException : Exception
{
    public MException(string Message)
        : base(Message)
    { }
}