using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;

using GIDOO_space;
using System.Windows.Media.Animation;
//using Accessibility;
using System.Security.Policy;

namespace GNPXcore{

    // UInt128 is available in .net7.0.  Update Bit81 to UInt128 version
    //  ... Currently under development/verification.

    public class Bit81{            //UInt128 ver.
    //Bit expression of the entire SUDOKU board.
    // Attributes are ID and digit.
    // Generator functions are from various class entities.
    // Functions are bit operation(arithmetic, logical, string functions).

        static private readonly UInt128 all_1= UInt128.MaxValue;
        static private readonly UInt128 _b32 = uint.MaxValue;
        static private readonly UInt128 _b9  = 0x1FF;
        static private readonly UInt128 _b1  = 1;

        // ----- property -----
        private UInt128 _BP;
        public  int     ID;    // usage differs depending on the algorithm.
        public  int     no;    // 0-8

        public int Count{ get{ return BitCount(); } }   //count propertie
        //============================================================== Generator
        static Bit81(){ }

        public Bit81(){ _BP=0; }
        public Bit81( UInt128 X ){ _BP=X ; }
        public Bit81( int rc ): this(){ BPSet(rc); }
        public Bit81( Bit81 P, int ID=int.MaxValue ): this(){
            if( ID!=int.MaxValue )  this.ID = ID;
            this._BP=P._BP;
        }






        public Bit81( List<UCell> Xlst ): this(){
            Xlst.ForEach( P => _BP |= (UInt128)1<<P.rc );
        }
        public Bit81( List<UCell> Xlst, int F, int FreeBC=-1 ):this(){
            if( FreeBC<0 ) Xlst.ForEach( P=>{ if((P.FreeB&F)>0) _BP |= ((UInt128)1<<P.rc); });
            else Xlst.ForEach(P=>{ if((P.FreeB&F)>0 && P.FreeBC==FreeBC) _BP |= (_b1<<P.rc); });
        }
        public Bit81( List<UCell> X, int noB ):this(){
            foreach( var P in X.Where(p=>(p.FreeB&noB)>0) ) _BP |= (_b1<<P.rc);
        }

        public Bit81( bool all1 ): this(){ _BP = all_1; }

        //==============================================================  Functions

        public UInt128 Get_BitExpression() => _BP;
        public void Clear() => _BP=0;
        public void BPSet( int rc )     => _BP |= ((UInt128)1<<rc);
        public void BPSet( Bit81 sdX )  => _BP |= sdX._BP;
        public void BPReset( int rc )   =>  _BP &= ((UInt128)1<<rc)^all_1;
        public void BPReset( Bit81 sdX ) => _BP &= sdX._BP^all_1;

        public int  AggregateFreeB( List<UCell> XLst )    => XLst.Aggregate( 0, (Q,q) => Q|q.FreeB );

        static public Bit81 operator|( Bit81 A, Bit81 B ) => new Bit81( A._BP | B._BP );

        static public Bit81 operator&( Bit81 A, Bit81 B ) => new Bit81( A._BP & B._BP );

        static public Bit81 operator^( Bit81 A, Bit81 B ) => new Bit81( A._BP ^B._BP );

        static public Bit81 operator-( Bit81 A, Bit81 B ) => new Bit81( A._BP & (B._BP^all_1) );

        static public bool operator==( Bit81 A, Bit81 B ) => ( A._BP == B._BP );

        static public bool operator!=( Bit81 A, Bit81 B ) => ( A._BP != B._BP );

        public override int GetHashCode() => _BP.GetHashCode();
        public int  CompareTo(Bit81 B)    => this._BP.CompareTo(B._BP );

        public bool IsHit( int rc )       => (_BP&(UInt128)1<<rc) > 0;
        public bool IsHit( Bit81 sdk )    => (_BP & sdk._BP)>0;

        public bool IsHit( List<UCell> LstP ) => LstP.Any( P => IsHit(P.rc) );

        public bool IsZero()              =>  (_BP== 0); 
    
        public bool IsNotZero()           => !IsZero();

        public override bool Equals( object obj ) => _BP == ((Bit81)obj)._BP;


        public int  BitCount(){
            UInt128 U128=_BP;
            uint UI;
            int  cnt=0;
            for( int k=0; k<4; k++ ){
                UI = (uint)(U128&_b32);
                if( UI != 0 )  cnt += UI.BitCount();
                U128 >>= 32;
            }
            return cnt;
        }
     
        public int FindFirst_rc(){
            UInt128 w = _BP;
            for( int rc=0; rc<81; rc++ ){
                if( (w&_b9) == 0 ){ w>>=9; rc+=8; continue;}
                if( (w&_b1)>0 )  return rc;
                w >>= 1;
            }

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
                if( IsHit(r*9+c) ) bp |= 1<<nx;
            }
            return  bp;
        }
         
        public IEnumerable<int> IEGetRC( int mx=81){
            UInt128  B=_BP;
            for(int m=0; m<mx; m++){
                if( (B&1)>0 ) yield return m;
                B=B>>1;
            }
        }
        public List<int> ToList( ){
            List<int> rcList = new List<int>();
            UInt128 bp = _BP;
            for(int k=0; k<81; k++){ if( (bp&1)>0) rcList.Add(k); bp>>=1; }
            return rcList;
        }



        public int Get_RowBitPatten( int rx ){
            uint bp = (uint)( this._BP>>(rx*9) );
            return (int)(bp&0x1FF);
        }
        public int GetColumnPattern( int cx ){
            int bp=0, k=0;
            for(int rc=cx; rc<81; rc+=9){
                if( IsHit(rc) ) bp |= 1<<k;
                k++;
            }
            return bp;
        }

        static private int sft9=7<<9, sft18=7<<18; 
        public int Get_blockBitPattern( int bx ){
            //Arrange the cells in order of block
            int bp = (int)( this._BP >> (bx/3*9+(bx%3)*3));
            int bPat = (bp&7) | ((bp&sft9)>>6) | ((bp&sft18)>>12);
            return bPat;
        }

        public int Get_RowColumnBlock(){
            int rcb=0;
            foreach(var rc in IEGetRC())   rcb |= rc.ToRCB_BitRep();

            return rcb;
        }


        public override string ToString(){
            string st="";
            for(int n=0; n<3; n++){
                int bp = (int)( _BP >> (n*27) );
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

        public string ToString128(){
            string st="";
              
            UInt128 w = this._BP;
            for(int k=0; k<128; k++){
                st += (w&1)>0? $"{k%8+1}": "."; 
                if(k%8==7) st += " ";
                w >>=1 ;
            }
            return st;
        }

        public string ToRCString(){
            string st="";
            for(int n=0; n<3; n++){
                int bp = (int)( _BP >> (n*27) );
                for(int k=0; k<27; k++){
                    if((bp&(1<<k))==0)  continue;
                    int rc=n*27+k;
                    st += $" {rc.ToRCString()}";
                }
            }
            return st;
        }

        public string ToString_SameHouseComp() => ToRCString().ToString_SameHouseComp();
/*
        static public string ToRCString_withComp( this Bit81 B81 ){   
            string st = B81.IEGet_rc().Aggregate("",(a,b)=> a+" "+b.ToRCString() );
            return st.ToString_SameHouseComp();
        }
*/
    }  



#if RegularVersion    
    public class Bit981{
        //bit81×9 digits
        static readonly int  sz=9;          // size(fixed)
//###        static private int[] __RCBpattern;  // rc -> bit expression of Row/Column/Block

        public  Bit81[]      _BQ;           // bit expression of the rc position of the cell
        public  int[]        RCBbit;        // rcb bit expression
        public  int          noBit=0;       // bit expression of digits containing cell elements

        public int Count{ get{ return BitCount(); } }

        public Bit981( bool eSet=true ){
            _BQ=new Bit81[9]; RCBbit=new int[9];
            if( eSet ) for(int n=0; n<sz; n++) _BQ[n]=new Bit81();
        }

        public Bit981(Bit981 Q ): this(){
            this.noBit = Q.noBit;
            for(int n=0; n<sz; n++) this._BQ[n]=Q._BQ[n];
        }

        public Bit981( int SetBit ): this( eSet:false ){
            this.noBit = SetBit&0x1FF;
            for(int n=0; n<sz; n++) if( (SetBit&(1<<n))>0 ) this._BQ[n]=new Bit81();;
        }
        public Bit981(Bit81 P): this(){
            int no = P.no;
            this._BQ[no] = P;
            if( P.IsNotZero() ) noBit |= (1<<no);
        }

        public Bit981( List<UCell> pBOARD, bool eSet=true ){
            _BQ = new Bit81[9];
            for( int no=0; no<9; no++ ){ 
                int noB = (1<<no);
                Bit81 BD = new Bit81(pBOARD,noB);   
                if( eSet || BD.IsNotZero() ) _BQ[no] = BD;
            }
        }
         
        public Bit981(UGLink UGL): this(){
            Bit981 B=new Bit981();

            if(UGL.rcBit81 is Bit81){
                int no=UGL.rcBit81.no;
                B._BQ[no] = UGL.rcBit81;
                foreach(int rc in UGL.rcBit81.IEGet_rc())  B._BQ[no].BPSet(rc);
                if( UGL.rcBit81.IsNotZero() ) noBit |= (1<<no);
            }
            else{ 
                UCell uc=UGL.UC as UCell;
                foreach(var n in uc.FreeB.IEGet_BtoNo()){
                    B._BQ[n].BPSet(uc.rc);
                    noBit |= (1<<n);
                }
            }
        }

        public void Clear( bool nullSet=false ){
            if(nullSet){ for(int n=0; n<sz; n++) _BQ[n]=null; } 
            else{ for(int n=0; n<sz; n++){ this._BQ[n].Clear(); RCBbit[n]=0; } }
            noBit=0;
        }
        public void BPSet(int no, int rc, bool tfbSet=false){
            _BQ[no].BPSet(rc); noBit |= (1<<no);
            if(tfbSet) rcbBitSet(no,rc);
        }
        public void BPSet(int no, Bit81 sdX, bool tfbSet=false){
            _BQ[no] |= sdX; noBit |= (1<<no);
            if(tfbSet){
                foreach(var rc in sdX.IEGetRC())  rcbBitSet(no,rc);
            }
        }
        public void BPReset(int no, int rc){
            _BQ[no].BPReset(rc);
            if( _BQ[no].IsZero() ) noBit.BitReset(no);
        }
        public void rcbBitSet(int no, int rc){           
            RCBbit[no] |= rc.ToRCB_BitRep();     //(1<<(rc/9)) | (1<<(rc%9+9)) | (1<<((rc/27*3+(rc%9)/3)+18));
        }
      //public Bit81 Get_BP81A2(int n0, int n1){ return _BQ[n0]&_BQ[n1]; }
/*
        public void rcbBitReset(int n, int h){ 
            if(n!=0xF) RCBbit[n].BitReset(h); 
        }
*/
        public Bit981 Copy(){ 
            Bit981 Scpy=new Bit981();
            for(int n=0; n<sz; n++) Scpy._BQ[n] = new Bit81(_BQ[n]);
            Scpy.noBit = this.noBit;
            return Scpy;
        }

        static public Bit981 operator|(Bit981 A, Bit981 B){
            Bit981 C = new Bit981();
            for(int n=0; n<sz; n++){ C._BQ[n] = A._BQ[n] | B._BQ[n]; }
            __Set_nzBit(C);
            return C;
        }
        static public Bit981 operator&(Bit981 A, Bit981 B){
            Bit981 C = new Bit981();
            for(int n=0; n<sz; n++) C._BQ[n] = A._BQ[n] & B._BQ[n];
            __Set_nzBit(C);
            return C;
        }
        static public Bit981 operator^(Bit981 A, Bit981 B){
            Bit981 C = new Bit981();
            for(int n=0; n<sz; n++) C._BQ[n] = A._BQ[n] ^ B._BQ[n];
            __Set_nzBit(C);
            return C;
        }
        static public Bit981 operator-(Bit981 A, Bit981 B){
            Bit981 C = new Bit981();
            for(int n=0; n<sz; n++) C._BQ[n] = A._BQ[n] - B._BQ[n];
            __Set_nzBit(C);
            return C;
        }
        static private void __Set_nzBit(Bit981 C){
            int noBit=0;
            for(int n=0; n<sz; n++){
                if( C._BQ[n].IsZero() ) noBit &= ((1<<n)^0x7FFFFFFF);
                else noBit |= (1<<n);
            }
            C.noBit=noBit;
        }

        static public bool operator==(Bit981 A, Bit981 B){
            try{
                if(A.noBit!=B.noBit)  return false;
                if(B is Bit981){
                    for(int k=0; k<sz; k++){ if(A._BQ[k]!=B._BQ[k]) return false; }
                    return true;
                }
                return false;
            }
            catch(NullReferenceException ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); return false; }
        }
        static public bool operator!=(Bit981 A, Bit981 B){
            try{
                if(A.noBit!=B.noBit)  return true;
                if(B is Bit981){
                    for(int k=0; k<sz; k++){ if(A._BQ[k]!=B._BQ[k]) return true; }
                    return false;
                }
                else return true;
            }
            catch(NullReferenceException ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); return true; }
        }

        public override int GetHashCode(){
            uint hc=0;
            uint P = (uint)_BQ[0].GetHashCode();
            for(int k=1; k<sz; k++){ hc ^= (uint)_BQ[k].GetHashCode()^P^(uint)(3<<k); }
            return (int)hc;
        }

        public uint CompareTo(Bit981 B){
            for(int n=0; n<sz-1; n++){
                if(this._BQ[n]==B._BQ[n])  return (uint)(this._BQ[n].CompareTo(B._BQ[n]));
            }
            return (uint)(this._BQ[sz-1].CompareTo(B._BQ[sz-1]));
        }


        public int IsHit(int rc){
            int H=0;
            for(int n=0; n<9; n++){ if(IsHit(n,rc)) H |= (1<<n); }
            return H;
        }

        //public bool IsHit(int no, int rc){
        //    return ((_BQ[no]._BP[rc/27]&(1<<(rc%27)))>0);
        //}

        public bool IsHit(int no, int rc) => _BQ[no].IsHit(rc);

        public bool IsHit(int no, Bit81 A){
            if(_BQ[no].IsHit(A))  return true;
            return false;
        }
        public Bit81 CompressToHitCells(){
            Bit81 Q=new Bit81();
            foreach(var n in noBit.IEGet_BtoNo()) Q |= _BQ[n];
            return Q;
        }

        public bool IsZero(){
            if(noBit==0)  return true;
            for(int k=0; k<sz; k++){ if( _BQ[k].IsNotZero() )  return false; }
            return true;
        }    
        public bool IsNotZero() => !IsZero();
        public override bool Equals(object obj){
            Bit981 A = obj as Bit981;
            if(A==null)  return false;
            for(int k=0; k<sz; k++){ if(_BQ[k]!=A._BQ[k]) return false; }
            return true;
        }       
        public int  BitCount(){
            int bc=0;
            foreach(int n in noBit.IEGet_BtoNo()) bc += _BQ[n].BitCount();
            // for(int k=0; k<sz; k++) bc+=_BQ[k].BitCount();
            return bc;
        } 
        
        public int GetBitPattern_tfnx(int n, int h){
            Bit81 P=_BQ[n];        
            int r=0, c=0, tp=h/9, fx=h%9, bp=0;
            for(int nx=0; nx<9; nx++){
                switch(tp){
                    case 0: r=fx; c=nx; break;//row
                    case 1: r=nx; c=fx; break;//column
                    case 2: r=(fx/3)*3+nx/3; c=(fx%3)*3+nx%3; break;//block
                }
                if(P.IsHit(r*9+c)) bp|=1<<nx;
            }
            return  bp;
        }
        public int GetBitPattern_rcN(int rc){
            int bp=0;
            foreach(var n in noBit.IEGet_BtoNo()){ if(_BQ[n].IsHit(rc))  bp|=1<<n; }
            return bp;
        }

        public override string ToString(){
            string st="nonZero:"+noBit.ToBitString(9)+"\r";
            for(int no=0; no<sz; no++){
                st += string.Format("no:{0} {1}", no, _BQ[no]) + "\r";
            }
            return st;
        }
    }

    public class Bit324{    //324=81*4
      //Bit representations of arbitrary length
        public int   ID;
        static public readonly int len=324;
        static public readonly int sz;
        public readonly uint[] _BP;

        public int Count{ get{ return BitCount(); } }

        static Bit324(){ sz=(len-1)/32+1; }
        public Bit324(){ _BP=new uint[sz]; }
        public Bit324(Bit324 P):this(){ for(int k=0; k<sz; k++) this._BP[k]=P._BP[k]; }
        public Bit324(uint[] _BPw):this(){
            if(_BPw.GetLength(0)<sz) WriteLine($"Length Error:{_BP.GetLength(0)}");
            for(int k=0; k<sz; k++) this._BP[k]=_BPw[k];
        }

        public Bit324(bool all):this(){
            if(all) for(int k=0; k<sz; k++) this._BP[k]= 0xFFFFFFFF;
        }
         
        public Bit324(UGLink UGL){
            Bit324 B=new Bit324();
            if(UGL.rcBit81 is Bit81){
                int no81=UGL.rcBit81.no*81;
                foreach(int k in UGL.rcBit81.IEGet_rc()) B.BPSet(no81+k);
            }
            else{ 
                UCell uc=UGL.UC as UCell;
                foreach(var no in uc.FreeB.IEGet_BtoNo()) B.BPSet(no*81+uc.rc);
            }
        }

        public void Clear(){ for(int k=0; k<sz; k++) this._BP[k]= 0; }
//        public void BPSet(int rc){ _BP[rc/32] |= (uint)(1<<(rc%32)); }
        public void BPSet(int rcX){
            try{
                _BP[rcX/32] |= (uint)(1<<(rcX%32));
            }
            catch(Exception e){
                WriteLine(e.Message+"\r"+e.StackTrace);
            }      
        }

        public void BPSet(Bit324 sdX){ for(int k=0; k<sz; k++) _BP[k] |= sdX._BP[k]; }               
        public void BPReset(int rc){ _BP[rc/32] &= (uint)((1<<(rc%32))^0xFFFFFFFF); }

        public IEnumerable<int> IEGet_Index(){
            int rc=0;
            uint B;
            for(int k=0; k<sz; k++){
                rc=k*32;
                if((B=_BP[k])==0) continue;
                for(int m=0; m<32; m++){
                    if((B&1)==1) yield return (rc+m);
                    B=B>>1;
                }
            }
        }

        public Bit324 Copy(){ Bit324 Scpy=new Bit324(); Scpy.BPSet(this); return Scpy; }

        static public Bit324 operator|(Bit324 A, Bit324 B){
            Bit324 C = new Bit324();
            for(int k=0; k<sz; k++) C._BP[k] = A._BP[k] | B._BP[k];
            return C;
        }
        static public Bit324 operator&(Bit324 A, Bit324 B){
            Bit324 C = new Bit324();
            for(int k=0; k<sz; k++) C._BP[k] = A._BP[k] & B._BP[k];
            return C;
        }
        static public Bit324 operator^(Bit324 A, Bit324 B){
            Bit324 C = new Bit324();
            for(int k=0; k<sz; k++) C._BP[k] = A._BP[k] ^ B._BP[k];
            return C;
        }
        static public Bit324 operator-(Bit324 A, Bit324 B){
            Bit324 C = new Bit324();
            for(int k=0; k<sz; k++) C._BP[k] = A._BP[k] & (B._BP[k]^0xFFFFFFFF);
            return C;
        }

        static public bool operator==(Bit324 A, Bit324 B){
            try{
                if(B is Bit324){
                    for(int k=0; k<sz; k++){ if(A._BP[k]!=B._BP[k]) return false; }
                    return true;
                }
                return false;
            }
            catch(NullReferenceException ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); return true; }
        }
        static public bool operator!=(Bit324 A, Bit324 B){
            try{
                if(B is Bit324){
                    for(int k=0; k<sz; k++){ if(A._BP[k]!=B._BP[k]) return true; }
                    return false;
                }
                else return true;
            }
            catch(NullReferenceException ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); return true; }
        }

        public override int GetHashCode(){
            uint hc=0, p=7;
            for(int k=0; k<sz; k++){ hc ^= _BP[k]^p; p^=(p*3+997); }
            return (int)hc;
        }

        public uint CompareTo(Bit324 B){
            for(int k=0; k<sz; k++) if(this._BP[k]==B._BP[k])  return (this._BP[k]-B._BP[k]);
            return (this._BP[sz-1]-B._BP[sz-1]);
        }

        public bool IsHit(int rc){ return ((_BP[rc/32]&(1<<(rc%32)))>0); }
        public bool IsHit(Bit324 sdk){
            for(int k=0; k<sz; k++){ if((_BP[k]&sdk._BP[k])>0)  return true; }
            return false;
        }
        public bool IsHit(List<UCell> LstP){ return LstP.Any(P=>(IsHit(P.rc))); }

        public bool IsZero(){
            for(int k=0; k<sz; k++){ if(_BP[k]>0)  return false; }
            return true;
        }    
        public override bool Equals(object obj){
            Bit324 A = obj as Bit324;
            if(A==null)  return false;
            for(int k=0; k<sz; k++){ if(A._BP[k]!=_BP[k]) return false; }
            return true;
        }       
        public int  BitCount(){
            int bc=0;
            for(int k=0; k<sz; k++) bc+=_BP[k].BitCount();
            return bc;
        } 
        
        public int FindFirst_rc(){
            for(int rc=0; rc<81; rc++){ if(this.IsHit(rc)) return rc; }
            return -1;
        }

        public override string ToString(){
            string st="";
            int m=1;
            for(int n=0; n<sz; n++){
                uint bp =_BP[n];
                int tmp=1;
              for(int k=0; k<32; k++){
                    st += ((bp&tmp)>0)? ((m%9)+0).ToString(): "."; //Internal expression
                //  st += ((bp&tmp)>0)? ((m%9)+1).ToString(): "."; //External expression
                    tmp = (tmp<<1);
                    if((m%81)==0)     st += " /";
                    else if((m%9)==0) st += " ";
                    m++;
                }
            }
            return st;
        }
    }    

    public class Bit981B{
        static readonly int  sz=9;          // size(fixed)
        private Bit981 _true;
        private Bit981 _false;

        public Bit981B(){
            _true  = new Bit981();
            _false = new Bit981();
        }
        public void Clear(){
            _true.Clear(); _false.Clear();
        }
        public void BPSet_true(int no, int rc )  => _true.BPSet(no,rc);
        public void BPSet_false(int no, int rc ) => _false.BPSet(no,rc);

        public void BPReset_true(int no, int rc )  =>  _true.BPReset(no,rc);
        public void BPReset_false(int no, int rc ) =>  _false.BPReset(no,rc);

        public void BPSet( int no, int rc, (bool,bool) _tf ){ 
            var (_t,_f)=_tf;
            if(_t)  _true.BPSet(no,rc);
            if(_f)  _false.BPSet(no,rc);
        }
        public void BPSetRev( int no, int rc, (bool,bool) _tf ){ 
            var (_t,_f)=_tf;
            if(_f)  _true.BPSet(no,rc);
            if(_t)  _false.BPSet(no,rc);
        }

        public bool IsTrue( int no, int rc ) => _true.IsHit(no,rc);
        public bool IsFalse( int no, int rc ) => _true.IsHit(no,rc);
        public (bool,bool) GetValue( int no, int rc){
            bool _t=_true.IsHit(no,rc);
            bool _f=_false.IsHit(no,rc);
            return (_t,_f);
        }

    }
#endif

}