using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;

using GIDOO_space;
using System.Windows.Media.Animation;
//using Accessibility;
using System.Security.Policy;



#if false
namespace GNPXcore{
    
    // UInt128 is available in .net7.0. Update Bit81 to UInt128 version

    public class Bit81_z{
    //Bit expression of the entire SUDOKU board.
    // Attributes are ID and digit.
    // Generator functions are from various class entities.
    // Functions are bit operation(arithmetic, logical, string functions).

        public readonly int[] _BP;
        public int   ID;    // usage differs depending on the algorithm.
        public int   no;    // 0-8
        public int Count{ get{ return BitCount(); } }   //count propertie

        //============================================================== Generator
        public Bit81_z(){ _BP=new int[3]; }
        public Bit81_z( int rc ): this(){ BPSet(rc); }
        public Bit81_z( Bit81_z P, int ID=int.MaxValue ): this(){
            if( ID!=int.MaxValue )  this.ID = ID;
            this._BP[0]=P._BP[0]; this._BP[1]=P._BP[1]; this._BP[2]=P._BP[2];   //copy of value
        }
        public Bit81_z( int[] _BPw ): this(){
            if(_BPw.GetLength(0)<3) WriteLine($"Length Error:{_BP.GetLength(0)}");
            this._BP[0]=_BPw[0]; this._BP[1]=_BPw[1]; this._BP[2]=_BPw[2];      //copy of value
        }
        public Bit81_z( List<UCell> X ): this(){
            X.ForEach(P=>{ _BP[P.rc/27] |= (1<<(P.rc%27)); });
        }
        public Bit81_z( List<UCell> X, int F, int FreeBC=-1 ):this(){
            if(FreeBC<0) X.ForEach(P=>{ if((P.FreeB&F)>0) _BP[P.rc/27] |= (1<<(P.rc%27)); });
            else X.ForEach(P=>{ if((P.FreeB&F)>0 && P.FreeBC==FreeBC) _BP[P.rc/27] |= (1<<(P.rc%27)); });
        }
        public Bit81_z( List<UCell> X, int noB ):this(){
            X.ForEach(P=>{ if((P.FreeB&noB)>0) _BP[P.rc/27] |= (1<<(P.rc%27)); });
        }

        public Bit81_z( bool all1 ): this(){
            if(all1) this._BP[0]=this._BP[1]=this._BP[2]= 0x7FFFFFF;
        }
/*
        public int Get_rcbBitRep( Bit81_z rcB, int ID ){
            this.ID=ID;
            int rcbB=0;
            foreach( var rc in rcB.IEGet_rc() )  rcbB |= rc.ToRCB_BitRep();
            return  rcbB;
        }
*/
        //==============================================================  Functions
        public void Clear(){ _BP[0]=_BP[1]=_BP[2]=0; }
        public void BPSet( int rc ){ _BP[rc/27] |= (1<<(rc%27)); }
        public void BPSet( Bit81_z sdX ){ for(int nx=0; nx<3; nx++) _BP[nx] |= sdX._BP[nx]; }
        public void BPReset( int rc ){ _BP[rc/27] &= ((1<<(rc%27))^0x7FFFFFF); }
        public void BPReset( Bit81_z sdX ){ for(int nx=0; nx<3; nx++) _BP[nx] &= (sdX._BP[nx]^0x7FFFFFF); }
/*
        public int  AggregateFreeB( List<UCell> XLst ){
            return this.IEGet_rc().Aggregate(0,(Q,q)=>Q|XLst[q].FreeB);
        }      
*/
        static public Bit81_z operator|( Bit81_z A, Bit81_z B ){
            Bit81_z C = new Bit81_z();
            for(int nx=0; nx<3; nx++) C._BP[nx] = A._BP[nx] | B._BP[nx];
            return C;
        }
        static public Bit81_z operator& (Bit81_z A, Bit81_z B ){
            Bit81_z C = new Bit81_z();
            for(int nx=0; nx<3; nx++) C._BP[nx] = A._BP[nx]&B._BP[nx];
            return C;
        }
        static public Bit81_z operator^( Bit81_z A, Bit81_z B ){
            Bit81_z C = new Bit81_z();
            for(int nx=0; nx<3; nx++) C._BP[nx] = A._BP[nx] ^ B._BP[nx];
            return C;
        }
        static public Bit81_z operator^( Bit81_z A, int sdbInt ){
            Bit81_z C = new Bit81_z();	
            for(int nx=0; nx<3; nx++) C._BP[nx] = A._BP[nx] ^ sdbInt;
            return C;
        }
        static public Bit81_z operator-(Bit81_z A, Bit81_z B){
            Bit81_z C = new Bit81_z();
            for(int nx=0; nx<3; nx++) C._BP[nx] = A._BP[nx] & (B._BP[nx]^0x7FFFFFF);
            return C;
        }

        static public bool operator==( Bit81_z A, Bit81_z B ){
            try{
                if(!(A is Bit81_z) || !(B is Bit81_z))  return false;
                for(int nx=0; nx<3; nx++){ if(A._BP[nx]!=B._BP[nx]) return false; }
                return true;
            }
            catch(NullReferenceException ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); return true; }
        }
        static public bool operator!=( Bit81_z A, Bit81_z B ){
            try{
                if(!(A is Bit81_z) || !(B is Bit81_z))  return true;
                for(int nx=0; nx<3; nx++){ if(A._BP[nx]!=B._BP[nx]) return true; }
                return false;
            }
            catch(NullReferenceException ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); return true; }
        }

        public override int GetHashCode(){ return (_BP[0]^ (_BP[1]*1301)^ (_BP[2]*6577)); }
        public int CompareTo(Bit81_z UB){
            if( this._BP[0] != UB._BP[0] )  return (this._BP[0]-UB._BP[0]);
            if( this._BP[1] != UB._BP[1] )  return (this._BP[1]-UB._BP[1]);
            if( this._BP[2] != UB._BP[2] )  return (this._BP[2]-UB._BP[2]);
            return 0;
        }

        public bool IsHit( int rc ) => ((_BP[rc/27]&(1<<(rc%27)))>0);
        public bool IsHit( Bit81_z sdk ){
            for(int nx=0; nx<3; nx++){ if((_BP[nx]&sdk._BP[nx])>0)  return true; }
            return false;
        }
        public bool IsHit( List<UCell> LstP ) => LstP.Any( P => IsHit(P.rc) );

        public bool IsZero() => ( _BP[0]==0 && _BP[1]==0 && _BP[2]==0 ); 
    
        public bool IsNotZero() => !IsZero();

        public override bool Equals( object obj ){
            Bit81_z A = obj as Bit81_z;
            for(int nx=0; nx<3; nx++){ if(A._BP[nx]!=_BP[nx]) return false; }
            return true;
        }       
        public int  BitCount(){
            int bc = _BP[0].BitCount() + _BP[1].BitCount() + _BP[2].BitCount();
            return bc;
        } 
     
        public int FindFirst_rc(){
            for(int rc=0; rc<81; rc++){ if(this.IsHit(rc)) return rc; }
            return -1;
        }

        public int GetBitPattern_tfx( int h ){
            int r=0, c=0, tp=h/9, fx=h%9, bp=0;
            for(int nx=0; nx<9; nx++){
                switch(tp){
                    case 0: r=fx; c=nx; break;                      //row
                    case 1: r=nx; c=fx; break;                      //column
                    case 2: r=(fx/3)*3+nx/3; c=(fx%3)*3+nx%3; break;//block
                }
                if(IsHit(r*9+c)) bp|=1<<nx;
            }
            return  bp;
        }
         
        public IEnumerable<int> IEGetRC(){
            int rc=0, B;
            for(int k=0; k<3; k++){
                rc=k*27;
                if((B=_BP[k])==0) continue;
                for(int m=0; m<27; m++){
                    if((B&1)==1) yield return (rc+m);
                    B=B>>1;
                }
            }
        }
        public List<int> ToList(){
            List<int> rcList = new List<int>();
            for(int n=0; n<3; n++){
                int bp = _BP[n];
                for(int k=0; k<27; k++){ if((bp&(1<<k))>0) rcList.Add(n*27+k); }
            }
            return rcList;
        }

        static private int sft9=7<<9, sft18=7<<18;

        public int Get_RowBitPatten( int rx ){
            int bp = this._BP[rx/3]>>((rx%3)*9);
            return (bp&0x1FF);
        }
        public int GetColumnPattern( int cx ){
            int bp=0, k=0;
            for(int rc=cx; rc<81; rc+=9){
                if(IsHit(rc)) bp = (1<<k);
                k++;
            }
            return bp;
        }
        public int Get_blockBitPattern( int bx ){
            //Arrange the cells in order of block
            int bp = this._BP[bx/3]>>((bx%3)*3);
            int bPat = (bp&7) | ((bp&sft9)>>6) | ((bp&sft18)>>12);
            return bPat;
        }

        public int Get_RowColumnBlock(){
            int rcb=0;
            foreach(var rc in IEGetRC()){
                rcb |= rc.ToRCB_BitRep();
            }
            return rcb;
        }

        public override string ToString(){
            string st="";
            for(int n=0; n<3; n++){
                int bp =_BP[n];
                int tmp=1;
              for(int k=0; k<27; k++){
                    st += ((bp&tmp)>0)? ((k%9)+0).ToString(): "."; //Internal expression
                //  st += ((bp&tmp)>0)? ((k%9)+1).ToString(): "."; //External expression
                    tmp = (tmp<<1);
                    if(k==26)         st += " /";
                    else if((k%9)==8) st += " ";
                }
            }
            return st;
        }
        public string ToRCString(){
            string st="";
            for(int n=0; n<3; n++){
                int bp=_BP[n];
                for(int k=0; k<27; k++){
                    if((bp&(1<<k))==0)  continue;
                    int rc=n*27+k;
                    st += $" {rc.ToRCString()}";
                }
            }
            return st;
        }

        public string ToString_SameHouseComp() => ToRCString().ToString_SameHouseComp();
    }  


}
#endif