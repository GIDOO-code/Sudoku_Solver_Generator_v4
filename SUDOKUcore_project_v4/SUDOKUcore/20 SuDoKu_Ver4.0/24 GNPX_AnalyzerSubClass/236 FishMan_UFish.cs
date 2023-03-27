using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static System.Diagnostics.Debug;

using GIDOO_space;
using System.Windows.Controls;

namespace GNPXcore{

    public class FishMan{
        private List<UCell>   pBOARD;             //SuDoKu cells   (for reference)    
                
        private Bit81[]       pHouseCells{     get{ return AnalyzerBaseV2.HouseCells; } }
        private Bit81[]       pConnectedCells{ get{ return AnalyzerBaseV2.ConnectedCells; } }

        private int           sz;               //fish size (2D:Xwing 3D:SwordFish 4D:JellyFish 5D:Squirmbag 6D:Whale 7D:Leviathan)
        private int           no;               //digit

        private List<Bit81_withRCB>  HBLst=new List<Bit81_withRCB>(); //element cells of fish(bit expression)
        private bool extFlag;                   //3D or more sizes. ...



        public FishMan( AnalyzerBaseV2 AnB, int FMSize, int no, int sz, bool extFlag=false ){
            this.pBOARD = AnB.pBOARD;
            this.extFlag = extFlag;                          //sz>2 or ...                 
            this.no=no; this.sz=sz;

            Bit81 BPnoB = new Bit81(pBOARD,1<<no);
            for( int h=0; h<FMSize; h++ ){                   //crate element cells of fish
                Bit81 Q = pHouseCells[h] & BPnoB;
                if( Q.IsZero() )  continue;
                var R = new Bit81_withRCB( Q, ID:h ); 
                HBLst.Add( R );                     // Contains the same cells even in different "House".
                    //WriteLine( $"h:{h} R:{R}" );  #####
            }
            if( HBLst.Count < sz*2 ){ HBLst=null; return; }  //Need more than sz*2
                    //int kx=0;
                    //HBLst.ForEach( P => WriteLine( $" kx:{kx++} P.ID:{P.ID}  P{P}" ) );
        }

        public class Bit81_withRCB: Bit81{
            public int rcbB=0;
            public Bit81_withRCB( Bit81 rcB, int ID ): base(rcB, ID:ID){
                foreach( var rc in rcB.IEGet_rc() )  rcbB |= rc.ToRCB_BitRep();
            }
            public override string ToString( ){
                string st = $"rcbB:{rcbB.HouseToString()} ID:{ID}\n" + base.ToString();
                return st;
            }
        }




#region BaseSet
        public IEnumerable<UFish> IEGet_BaseSet( int BasesetFilter, bool FinnedFlag=false, bool EndoFlag=false ){
            if( HBLst==null )  yield break;

            bool basicFish = (BasesetFilter.BitCount()<=9) & !FinnedFlag;  //not F/M & notF/M
            int  BaseSelR  = 0x3FFFF ^ BasesetFilter;
#if RegularVersion
                                    GeneralLogicGen.ChkBas1=0;
                                    GeneralLogicGen.ChkBas2=0;
#endif
               //int kx=0;
               //HBLst.ForEach( P => WriteLine( $"kx:{kx++} {P}" ) );

            Bit81 Q;
            Combination cmbBas = new Combination( HBLst.Count, sz );
            int nxt = int.MaxValue;
            while( cmbBas.Successor(skip:nxt) ){
#if RegularVersion
                                    int chk1=++GeneralLogicGen.ChkBas1;
#endif
                int   usedLK=0;
                Bit81 BaseB81=new Bit81();
                Bit81 EndoFinB81=new Bit81();
                int rcbB=0;
                for( int k=0; k<sz; k++ ){
                    nxt=k;
                    int nx = cmbBas.Index[k];
                    Bit81_withRCB HBF = HBLst[nx];

                    if( ((1<<HBF.ID) & BasesetFilter) == 0 )  goto nxtCmb;  // not match RCB specification
                    if( (Q=BaseB81&HBF).IsNotZero() ){                   // overlap
                        if( !EndoFlag )   goto nxtCmb;                 // Endo Fish?
                        EndoFinB81 |= Q;                                   // overlaped cells
                    }
                    usedLK |= 1<<HBF.ID;                              // set house Number
                    BaseB81   |= HBF;                                    // BaseSet Rep.
                    rcbB   |= HBF.rcbB;                               // RCB Rep.
                    if( basicFish && k>0 && (rcbB&BaseSelR).BitCount()>sz ) goto nxtCmb; 
                }
                if( EndoFlag && EndoFinB81.IsZero() )  goto nxtCmb;

/*
  this code is not suitable?  Delete after confirming unnecessary.
                extFlag=false; 
                if( extFlag && !__IsLinked9(BaseB81) )  continue;
*/

#if RegularVersion
                                    int chk2=++GeneralLogicGen.ChkBas2;
#endif

                UFish UF = new UFish( no, sz, usedLK, BaseB81, EndoFinB81 );    //Baseset
                yield return UF;

              nxtCmb:
                continue;
            }
            yield break;

            // ----- inner function -----
            void __Debug_PattenPrint( UFish UF ){
                WriteLine( $"no={no} sz={sz}  BaseSet:{UF.HouseB.HouseToString()}" );
                Bit81 BPnoB=new Bit81(pBOARD,1<<no);
                string noST=" "+no.ToString();
                for(int r=0; r<9; r++ ){
                    string st="";
                    BPnoB.GetRowList(r).ForEach(p=>st+=(p==0? " .": noST));
                    st+=" ";
                    UF.BaseB81.GetRowList(r).ForEach(p=>st+=(p==0? " .": " B"));
                    st+=" ";
                    (BPnoB-UF.BaseB81).GetRowList(r).ForEach(p=>st+=(p==0? " .": " X"));
                    WriteLine(st);
                }
            }
        }
#endregion BaseSet



#region CoverSet
        public IEnumerable<UFish> IEGet_CoverSet( UFish UFish_BaseSet, int CoverSetFilter, bool FinnedFlag, bool CannFlag=false ){
            if( HBLst==null )  yield break;
//            if( !FinnedFlag )   yield break;  //#################

            var HCLst=new List<Bit81_withRCB>();
            {   // Select only the elements related to the BaseSet as candidates for the CoverSet.
                // This process is extremely effective.
                // Clearly considering the relationship between BaseSet and CoverSet.
                foreach( var P in HBLst.Where(q=>(UFish_BaseSet.HouseB&(1<<q.ID))==0) ){
                    if( ((1<<P.ID)&CoverSetFilter)==0 )  continue;
                    if( UFish_BaseSet.BaseB81.IsHit(P) )  HCLst.Add(P);
                }
            }
            if(HCLst.Count<sz) yield break;
/* ### 
                if( UFish_BaseSet.HouseB.HouseToString() == "r2 c2 b9" ){
                    int kx=0;
                    WriteLine( "\n----- HBLst" );
                    foreach( var BSet2 in HBLst ){
                        WriteLine( $"kx:{kx++}  {BSet2}" );
                    }

                    kx=0;
                    WriteLine( "\n+++++ HCLst" );
                    foreach( var Cov in HCLst ){
                        WriteLine( $"kx:{kx++}  {Cov}" );
                    }
                } 
*/ //### 
            Bit81 Q;
            Combination cmbCov=new Combination(HCLst.Count,sz);
            int nxt=int.MaxValue;
            while( cmbCov.Successor(skip:nxt) ){
#if RegularVersion
                                int chk1=++GeneralLogicGen.ChkCov1;
#endif


                if( sz==3 ){
                    if( HCLst[cmbCov.Index[0]].ID!=7  )  continue;      
                    if( HCLst[cmbCov.Index[1]].ID!=8  )  continue;
                    if( HCLst[cmbCov.Index[2]].ID!=15 )  continue;
                }

                int   usedLK=0;
                Bit81 CoverB81 = new Bit81();
                Bit81 CannFinB81 = new Bit81();
                for(int k=0; k<sz; k++ ){
                    nxt=k;
                    int nx=cmbCov.Index[k];                   
                    if( (Q=CoverB81&HCLst[nx]).IsNotZero() ){ //overlap
                        if( !CannFlag )  goto nxtCmb;
                        CannFinB81 |= Q;
                    }
                    usedLK |= 1<<HCLst[nx].ID;  //house number
                    CoverB81   |= HCLst[nx];        //Bit81
                }

                //If you just want to solve Sudoku, exclude the following code.
                if( CannFlag && CannFinB81.IsZero() ) goto nxtCmb; //Exclude other "fish" when CannFlag is true!


                // case no Fin, BaseSet is covered with CoverSet.
                // case with Fin, the part not covered by CoverSet of BaseSet is Fin.
                Bit81 FinB81 = UFish_BaseSet.BaseB81-CoverB81;     //(oprator- : difference set)
                if( FinnedFlag != (FinB81.Count>0) ) continue;      //
                UFish UF = new( UFish_BaseSet, usedLK, CoverB81, FinB81, CannFinB81 );
                //==================
                yield return UF;
                //------------------
              nxtCmb:
                continue;
            }
            yield break;
        }
    }
#endregion CoverSet



#region Fish
    public class UFish{
        public int      ID;             //ID
        public int      no;             //digit
        public int      sz;             //fish size (2D:Xwing 3D:SwordFish 4D:JellyFish 5D:Squirmbag 6D:Whale 7D:Leviathan)
        public int      HouseB=0;       //house No. in baseSet
        public Bit81    BaseB81=null;   //Bit expression of BaseSet cells

        public UFish    BaseSet=null;   //baseSet reference in CoverSet
        public int      HouseC=0;       //house No. in coverSet
        public Bit81    CoverB81=null;  //Bit expression of coverSet cells
        public Bit81    FinB81=null;    //Bit expression of fin cells
        public Bit81    EndoFinB81=null;   //Bit expression of EndoFinB81 cells
        public Bit81    CannFinB81=null;   //Bit expression of cannibalisticFin cells

        public UFish(){ }

        public UFish( int no, int sz, int HouseB, Bit81 BaseB81, Bit81 EndoFinB81 ){
            this.no=no;
            this.sz=sz;
            this.HouseB =HouseB;
            this.BaseB81=BaseB81;
            this.EndoFinB81=EndoFinB81;
        }
          
        public UFish( UFish BaseSet, int HouseC, Bit81 CoverB81, Bit81 FinB81, Bit81 CannFinB81 ){
            this.sz = BaseSet.sz;
            this.no = BaseSet.no;
            this.BaseSet =BaseSet;
            this.HouseB  =BaseSet.HouseB;
            this.HouseC  =HouseC;
            this.CoverB81=CoverB81;
            this.FinB81  =FinB81;
            this.CannFinB81 =CannFinB81;
        }
        public string ToString( string ttl ){
            string st = ttl + HouseB.HouseToString();
            return st;
        }

        public override string ToString( ){
            string st = $"UFish   ID : {ID}   no+ : {no+1}   sz : {sz} "; 
            string st2="";

            if( BaseSet is null ){ //BaseSet
                st2 += $"\n\n\n#######################";
                st2 += $"\n-- {st} Baseset --";
                st2 += $"\n--  BaseB81 : {BaseB81}\n--   HouseB : {HouseB.HouseToString()}";
            }
            else{
                st2 += $"\n++ {st} Coverset ++";
                st2 += $"\n++ CoverB81 : {CoverB81}\n++   HouseC : {HouseC.HouseToString()}";
                st2 += $"\n++   FinB81 : {FinB81}\n++  EndoFinB81 :{EndoFinB81}\n++  CannFinB81 :{CannFinB81}";
            }
            return st2;
        }
    }
#endregion Fish
}