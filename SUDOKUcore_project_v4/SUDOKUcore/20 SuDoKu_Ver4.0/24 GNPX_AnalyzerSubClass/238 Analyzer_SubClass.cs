using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;
using static System.Math;
using GIDOO_space;
using System.Windows.Media.Animation;
//using Accessibility;
using System.Security.Policy;
using System.Runtime.Serialization;

namespace GNPXcore{
    static public class G0{
        static private int _NSZ = 8192;
        static public long[] hashBase = new long[_NSZ];  
        static Random rnd = new Random(314);
        static G0(){
            Random rnd = new Random(314);
            for( int k=0; k<hashBase.Length; k++ ) hashBase[k] = rnd.NextInt64();
        }
    }


    public class Bit_N{
    //Bit expression of the entire SUDOKU board.
    // Attributes are ID and digit.
    // Generator functions are from various class entities.
    // Functions are bit operation(arithmetic, logical, string functions).

        public  readonly int[] _BP;
        public  int   ID;    // usage differs depending on the algorithm.
        public  int   n;
        private int  _BPsz;
        public int Count{ get{ return BitCount(); } }   //count propertie

        //============================================================== Generator
        public Bit_N(){ }

        public Bit_N( int n ){ this.n=n; _BPsz=(n-1)/32+1; _BP=new int[_BPsz]; }
        

        public Bit_N( int n, bool all1): this(n){
            if(all1) for( int k=0; k<_BPsz; k++ ) this._BP[k] = 0xFFFFFFF;
        }

        public Bit_N( int n, int kx ): this(n){
             _BP[kx/32] |= (int)(1<<(kx%32));
        }
        public Bit_N( int n, int[] Array_kx ): this(n){
            foreach( int kx in Array_kx ){ _BP[kx/32] |= (int)(1<<(kx%32)); }
        }

        //==============================================================  Functions
        public void Clear(){ for( int k=0; k<_BPsz; k++ ) _BP[k]=0; }
        public void BPSet(int rc){ _BP[rc/32] |= (int)(1<<(rc%32)); }
        public void BPReset(int rc){ _BP[rc/32] &= (int)((1<<(rc%32))^0xFFFFFFF); }   

        static public Bit_N operator|( Bit_N A, Bit_N B ){
            int szA=A._BPsz, szB=B._BPsz;
            if( szA!=szB )  throw new Exception("Argument Bit_N has different size");
            Bit_N C = new Bit_N(szA);
            for( int k=0; k<szA; k++) C._BP[k] = A._BP[k] | B._BP[k];
            return C;
        }
        static public Bit_N operator&( Bit_N A, Bit_N B ){
            int szA=A._BPsz, szB=B._BPsz;
            if( szA!=szB )  throw new Exception("Argument Bit_N has different size");
            Bit_N C = new Bit_N(szA);
            for( int k=0; k<szA; k++) C._BP[k] = A._BP[k] & B._BP[k];
            return C;
        }
        static public Bit_N operator^( Bit_N A, Bit_N B ){
            int szA=A._BPsz, szB=B._BPsz;
            if( szA!=szB )  throw new Exception("Argument Bit_N has different size");
            Bit_N C = new Bit_N(szA);
            for( int k=0; k<szA; k++) C._BP[k] = A._BP[k] ^ B._BP[k];
            return C;
        }
        static public Bit_N operator^( Bit_N A, int sdbInt ){
            int szA=A._BPsz;
            Bit_N C = new Bit_N(szA);
            for( int k=0; k<szA; k++) C._BP[k] = A._BP[k] ^ sdbInt;
            return C;
        }
        static public Bit_N operator-( Bit_N A, Bit_N B ){
            int szA=A._BPsz, szB=B._BPsz;
            if( szA!=szB )  throw new Exception("Argument Bit_N has different size");
            Bit_N C = new Bit_N(szA);
            for( int k=0; k<szA; k++) C._BP[k] = A._BP[k] & (B._BP[k]^0x7FFFFFF);
            return C;
        }

        static public bool operator==( Bit_N A, Bit_N B ){
            if( B is null )  return false;
            int szA=A._BPsz, szB=B._BPsz;
            if( szA != szB )  throw new Exception("Argument Bit_N has different size");

            if( !(A is Bit_N) || !(B is Bit_N) )  return false;
            for(int k=0; k<szA; k++){ if(A._BP[k]!=B._BP[k]) return false; }
            return true;
        }
        static public bool operator !=( Bit_N A, Bit_N B ){
            int szA = A._BPsz, szB = B._BPsz;
            if( szA != szB ) throw new Exception("Argument Bit_N has different size");

            if( !(A is Bit_N) || !(B is Bit_N)) return true;
            for (int k = 0; k < szA; k++) { if (A._BP[k] != B._BP[k]) return true; }
            return false;
        }
/*
        static public bool operator ==( int[] A_BP, int[] B_BP ){
            if( B_BP is null ) return false;
            int szA= A_BP.Length, szB = B_BP.Length;
            if( szA != szB ) throw new Exception("Argument Bit_N has different size");

            if (!(A_BP is int[]) || !(B_BP is int[])) return false;
            for (int k = 0; k < szA; k++) { if( A_BP[k] != B_BP[k]) return false; }
            return true;
        }

        static public bool operator !=(int[] A_BP, int[] B_BP){
            if( B_BP is null ) return false;
            int szA=A_BP.Length, szB=B_BP.Length;
            if( szA != szB ) throw new Exception("Argument Bit_N has different size");

            if( !(A_BP is int[]) || !(B_BP is int[])) return true;
            for( int k=0; k<szA; k++ ){ if( A_BP[k] != B_BP[k] ) return true; }
            return false;
        }
*/
        public int GetHashCode( Bit_N A ){ 
      //public override int GetHashCode( Bit_N A ){ //
            int szA=A._BPsz;
            long hs=0;
            for(int k=0; k<szA; k++) hs ^= ( (long)A._BP[k] ^ G0.hashBase[k] );
            int hsInt = (int)( hs ^ hs>>32 );
            return hsInt;
        }
        public int CompareTo( Bit_N B ){
            for( int k=0; k<_BPsz; k++ ){
                if(this._BP[k]==B._BP[k])  return (this._BP[k]-B._BP[k]);
            }
            return 0;
        }

        public bool IsHit( int rc ){ return ((_BP[rc/32]&(1<<(rc%32)))>0); }
        public bool IsHit(Bit_N sdk){
            for(int nx=0; nx<_BPsz; nx++){ if((_BP[nx]&sdk._BP[nx])>0)  return true; }
            return false;
        }
        public bool IsHit(List<UCell> LstP){ return LstP.Any(P=>(IsHit(P.rc))); }

        public bool IsZero(){
            for(int nx=0; nx<_BPsz; nx++){ if(_BP[nx]>0)  return false; }
            return true;
        }
        
        public bool IsNotZero() => !IsZero();

        public override bool Equals(object obj){
            Bit_N A = obj as Bit_N;
            for(int nx=0; nx<_BPsz; nx++){ if(A._BP[nx]!=_BP[nx]) return false; }
            return true;
        }       
        public int  BitCount(){
            int bc=0;
            for( int k=0; k<_BPsz; k++ ) bc+= _BP[k].BitCount();
            return bc;
        } 
        
        public int FindFirst_rc(){
            for(int rc=0; rc<n; rc++){ if(this.IsHit(rc)) return rc; }
            return -1;
        }

        
        public IEnumerable<int> IEGetRC(){
            int B;
            int rc=0;
            for(int k=0; k<_BPsz; k++){
                rc=k*32;
                if((B=_BP[k])==0) continue;
                for(int m=0; m<32; m++){
                    if((B&1)==1) yield return (rc+m);
                    B=B>>1;
                }
            }
        }
        public List<int> ToList(){
            List<int> rcList = new List<int>();
            for(int n=0; n<_BPsz; n++){
                int bp =_BP[n];
                if( bp==0 )  continue;
                int nn = n*32;
                for(int k=0; k<32; k++){
                    if( (bp&1)>0 ) rcList.Add(nn+k);
                    bp >>= 1;
                }
            }

            return rcList;
        }

        public override string ToString(){
            string st="";
            for(int n=0; n<_BPsz; n++){
                int bp =_BP[n];
                if( bp==0 )  continue;
                int nn = n*32;
                for(int k=0; k<32; k++){
                    if( (bp&1)>0 ) st += $" {nn+k}";
                    bp >>= 1;
                }
            }
            return st;
        }
    }  
}