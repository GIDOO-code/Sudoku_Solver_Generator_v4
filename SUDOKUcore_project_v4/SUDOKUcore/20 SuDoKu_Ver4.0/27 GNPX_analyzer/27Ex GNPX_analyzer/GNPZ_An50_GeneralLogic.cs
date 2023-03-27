using System;
using System.Linq;
using System.Collections.Generic;
using static System.Diagnostics.Debug;

using GIDOO_space;

namespace GNPXcore{

//*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*====*==*==*==*
//  Added description to code GeneralLogic and its auxiliary routine.
//  "GeneralLogic" completeness is about 30%.
//    Currently,it takes a few seconds to solve a size 3 problem.
//    As an expectation, I would like to solve a size 5 problem in a few seconds.
//    Probably need a new theory.
//*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*

    public partial class GeneralLogicGen: AnalyzerBaseV2{
        static public int   GLtrialCC=0;
        static public int   ChkBas0=0, ChkBas1=0, ChkBas2=0, ChkBas3=0, ChkBas4=0, ChkBas5=0, ChkBas6=0, ChkBas7=0;
        static public int   ChkCov1=0, ChkCov2=0;
        static public int   ChkBas3A=0, ChkBas3B=0;
        private int         stageNoMemo = -9;
        private UGLinkMan   UGLMan;
        private int         GLMaxSize;
        private int         GLMaxRank;

        public GeneralLogicGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){ }

        private bool break_GeneralLogic=false; //True if the number of solutions reaches the specified number.
        public bool GeneralLogic( ){                                //### GeneralLogic controler
            break_GeneralLogic=false;
            if( stageNo != stageNoMemo ){
				stageNoMemo = stageNo;
                GLMaxSize = (int)GNPX_App.GMthdOption["GenLogMaxSize"];
                GLMaxRank = (int)GNPX_App.GMthdOption["GenLogMaxRank"];
                UGLMan = new UGLinkMan(this);
 //               if(SDK_Ctrl.UGPMan==null)  SDK_Ctrl.UGPMan = new UPuzzleMan(stageNo:1);  //203202
                UGLMan.PrepareUGLinkMan( printB:false );
			}
            
            WriteLine( $"--- GeneralLogic --- trial:{++GLtrialCC}" );
            for(int sz=1; sz<=GLMaxSize; sz++ ){
                for(int rnk=0; rnk<=GLMaxRank; rnk++ ){ 
                    if(rnk>=sz) continue;
                    ChkBas1=0; ChkBas2=0; ChkBas3=0; ChkBas4=0; ChkBas5=0; ChkBas6=0; ChkBas7=0;
                    ChkBas3A=0; ChkBas3B=0;
                    ChkCov1=0; ChkCov2=0;

                    bool solB = GeneralLogicSolver(sz,rnk);
                    string st = solB? "++": "  ";

                    WriteLine($" {sz} {rnk} {st} Bas:({ChkBas1},{ChkBas2},{ChkBas3},{ChkBas4},{ChkBas5},{ChkBas6},{ChkBas7})/{ChkBas0} " +
                              $" Cov:{ChkCov2}/{ChkCov1}  interNum({ChkBas3A}/{ChkBas3B})");
                    if( solB | break_GeneralLogic ) return true;
                }
            }
            return (SolCode>0);
        }
        
        private bool GeneralLogicSolver( int sz, int rnk ){              //### GeneralLogic main routine
            if(sz>GLMaxSize || rnk>GLMaxRank)  return false;

            string st="";
            UGLMan.Initialize();
            foreach( var UBC in UGLMan.IEGet_BaseSet(sz,rnk) ){         //### BaseSet generator
                if( pAnMan.Check_TimeLimit() ) return false;

                var bas=UBC.HB981;
                foreach( var UBCc in UGLMan.IEGet_CoverSet(UBC,rnk) ){  //### CoverSet generator

                    for(int no=0; no<9; no++ ){
                        if( (UBCc.HC981._BQ[no]-UBCc.HB981._BQ[no]).IsNotZero() )  goto SolFound;
                    }
                    continue;

                  SolFound:
                    foreach( int n in UBCc.Can981.noBit.IEGet_BtoNo() ){
                        foreach( int rc in UBCc.Can981._BQ[n].IEGetRC() ){
                            pBOARD[rc].CancelB |= (1<<n);
                        }
                    }
                    if( SolInfoB ){
                        st=_generalLogicResult(UBCc);
                    }

                         //WriteLine("\r#################### "+st);
                         //UGLMan.Board_Check();       //for Research & Debug.

                    if( __SimpleAnalyzerB__ )  return true;
                    if( !pAnMan.SnapSaveGP(pPZL) ){ break_GeneralLogic=true; return true; }
                    }
                
              

            }
            return false;
        }

        private string _generalLogicResult( UBasCov UBCc ){
            try{
                Bit81 Q=new Bit81();
                foreach( var P in UBCc.covUGLs )  Q |= P.rcnBit.CompressToHitCells();
                foreach( var UC in Q.IEGetRC().Select(rc=>pBOARD[rc]))   UC.Set_CellBKGColor(SolBkCr2);
           
                for(int rc=0; rc<81; rc++ ){
                    int noB=UBCc.HB981.IsHit(rc);
                    if(noB>0) pBOARD[rc].Set_CellColorBkgColor_noBit(noB,AttCr,SolBkCr);
                }

                string msg = "\r     BaseSet: ";
                string msgB="";
                foreach( var P in UBCc.basUGLs ){
                    if(P.rcBit81 is Bit81) msgB += P.h.hToString()+$"#{(P.rcBit81.no+1)} ";
                    else msgB += P.UC.rc.ToRCString()+" ";
                }
                msg += ToString_SameHouseComp1(msgB);
               
                msg += "\r    CoverSet: ";
                string msgC="";
                foreach( var P in UBCc.covUGLs ){
                    if(P.rcBit81 is Bit81) msgC += P.h.hToString()+$"#{(P.rcBit81.no+1)} ";
                    else msgC +=P.UC.rc.ToRCString()+" ";
                }
                msg += ToString_SameHouseComp1(msgC);

                string st=$"GeneralLogic size:{UBCc.sz} rank:{UBCc.rnk}";
                Result = st;
                msg += $"\rChkBas:{ChkBas4}/{ChkBas1}  ChkCov:{ChkCov2}/{ChkCov1}";
                ResultLong = st+"\r "+msg;
                return st+"\r"+msg;
            }
            catch( Exception ex ){
                WriteLine(ex.Message);
                WriteLine(ex.StackTrace);
            }
            return "";
        }

        private string ToString_SameHouseComp1( string st ){
            char[] sep=new Char[]{ ' ', ',', '\t' };
            List<string> T=st.Trim().Split(sep).ToList();
            if(T.Count<=1) return st;
            List<_ClassNSS> URCBCell=new List<_ClassNSS>();
            T.ForEach(P=> URCBCell.Add(new _ClassNSS(P)));
            return ToString_SameHouseComp2(URCBCell);
        }
        private string ToString_SameHouseComp2( List<_ClassNSS> URCBCell){
            string st;
            bool cmpF=true;
            do{
                cmpF=false;
                foreach( var P in URCBCell ){
                    List<_ClassNSS> Qlst=URCBCell.FindAll(Q=>(Q.stRCB==P.stRCB && Q.stNum[0]==P.stNum[0]));
                    if(Qlst.Count>=2){
                        st=Qlst[0].stNum[0].ToString();
                        Qlst.ForEach(Q=>st+=Q.stNum.Substring(1,Q.stNum.Length-1));
                        _ClassNSS R=URCBCell.Find(Q=>(Q.stRCB==P.stRCB));
                        R.stNum = st;
                        foreach(var T in Qlst.Skip(1)) URCBCell.Remove(T);
                        cmpF=true;
                        break;
                    }

                    Qlst=URCBCell.FindAll(Q=>(Q.stNum==P.stNum && Q.stRCB[0]==P.stRCB[0]));
                    if(Qlst.Count>=2){
                        st=Qlst[0].stRCB[0].ToString();
                        Qlst.ForEach(Q=>st+=Q.stRCB.Substring(1,Q.stRCB.Length-1));
                        _ClassNSS R=URCBCell.Find(Q=>(Q.stNum==P.stNum));
                        R.stRCB = st;
                        foreach(var T in Qlst.Skip(1)) URCBCell.Remove(T);
                        cmpF=true;
                        break;
                    }
                }

            }while(cmpF);
            st="";
            URCBCell.ForEach(P=> st+=(P.stRCB+P.stNum+" ") );
            return st;
        }
        private class _ClassNSS{
            public int sz;
            public string stRCB;
            public string stNum="  ";
            public _ClassNSS( int sz, string stRCB, string stNum ){
                this.sz=sz; this.stRCB=stRCB; this.stNum=stNum;
            }
            public _ClassNSS( string st ){
                try{
                    sz=1; 
                    if(st.Length>=2) stRCB=st.Substring(0,2);
                    if(st.Length>=4) stNum=st.Substring(2,2);
                }
                catch(Exception){ }
            }
        }
    }  

    public class UBasCov{
        public Bit324       usedLK;
        public List<UGLink> basUGLs; //
        public List<UGLink> covUGLs; //
        public Bit981 HB981;
        public Bit981 HC981;
        public Bit981 Can981;
        public int    rcCan;
        public int    noCan;
        public int    sz;
        public int    rnk;

        public UBasCov( List<UGLink> basUGLs, Bit981 HB981, int sz, Bit324 usedLK ){
            this.basUGLs=basUGLs; this.HB981=HB981; this.sz=sz; this.usedLK=usedLK;
        }
 
        public void addCoverSet( List<UGLink> covUGLs, Bit981 HC981, Bit981 Can981, int rnk ){
            this.covUGLs=covUGLs; this.HC981=HC981; this.Can981=Can981; this.rnk=rnk;
        }
        public override string ToString(){
            string st="";
            foreach( var UGL in basUGLs){
                if(UGL.rcBit81 is Bit81){   // RCB
                    int no=UGL.rcBit81.no;
                    st += string.Format("Bit81: no:{0}  {1}\r", no, UGL.rcBit81 );
                }
                else{   // Cell
                    UCell UC=UGL.UC;
                    st += string.Format("UCell: {0}\r", UC );
                }
            }
            return st;
        }
    }

}

#if false
1..4....8.9.1...5.....63.....13.5.79..3...8..76.2.94.....75.....1...6.4.8....4..2
--- GeneralLogic --- trial:1
 1 0 ++ Bas:(83,0,0,0)/83  Cov:1/70  interNum(0/0)
--- GeneralLogic --- trial:2
 1 0    Bas:(103,0,0,0)/226  Cov:0/80  interNum(0/0)
 2 0 ++ Bas:(302,224,59,12)/3894  Cov:1/337  interNum(0/0)
--- GeneralLogic --- trial:3
 1 0    Bas:(103,0,0,0)/4037  Cov:0/72  interNum(0/0)
 2 0 ++ Bas:(495,369,106,24)/10078  Cov:1/496  interNum(0/0)
--- GeneralLogic --- trial:4
 1 0    Bas:(103,0,0,0)/10221  Cov:0/64  interNum(0/0)
 2 0    Bas:(601,414,116,46)/18738  Cov:0/511  interNum(0/0)
 2 1 ++ Bas:(276,221,141,50)/21270  Cov:1/26826  interNum(17/62)
--- GeneralLogic --- trial:5
 1 0    Bas:(103,0,0,0)/21413  Cov:0/62  interNum(0/0)
 2 0    Bas:(595,409,115,46)/29930  Cov:0/503  interNum(0/0)
 2 1    Bas:(995,719,515,206)/39963  Cov:0/102852  interNum(44/171)
 3 0 ++ Bas:(2615,2350,488,14)/232225  Cov:1/15031  interNum(0/0)
--- GeneralLogic --- trial:6
 1 0    Bas:(103,0,0,0)/232368  Cov:0/58  interNum(0/0)
 2 0    Bas:(598,400,109,46)/240896  Cov:0/496  interNum(0/0)
 2 1    Bas:(1008,713,505,211)/250929  Cov:0/95975  interNum(43/166)
 3 0 ++ Bas:(1732,1539,251,10)/379859  Cov:1/5287  interNum(0/0)
--- GeneralLogic --- trial:7
 1 0    Bas:(103,0,0,0)/380002  Cov:0/56  interNum(0/0)
 2 0    Bas:(639,428,116,46)/389008  Cov:0/512  interNum(0/0)
 2 1    Bas:(984,689,506,219)/399106  Cov:0/80559  interNum(46/160)
 3 0 ++ Bas:(2846,2495,458,15)/612945  Cov:1/10197  interNum(0/0)
--- GeneralLogic --- trial:8
 1 0    Bas:(103,0,0,0)/613088  Cov:0/52  interNum(0/0)
 2 0 ++ Bas:(643,421,108,46)/621676  Cov:1/438  interNum(0/0)
--- GeneralLogic --- trial:9
 1 0    Bas:(103,0,0,0)/621819  Cov:0/48  interNum(0/0)
 2 0    Bas:(633,399,108,46)/630999  Cov:0/364  interNum(0/0)
 2 1 ++ Bas:(469,361,251,100)/635484  Cov:1/37825  interNum(22/84)
--- GeneralLogic --- trial:10
 1 0    Bas:(103,0,0,0)/635627  Cov:0/44  interNum(0/0)
 2 0    Bas:(646,397,108,46)/644814  Cov:0/346  interNum(0/0)
 2 1    Bas:(993,651,480,223)/654912  Cov:0/61890  interNum(42/155)
 3 0    Bas:(5160,4295,682,36)/1028704  Cov:0/8550  interNum(0/0)
 3 1 ++ Bas:(1769,1232,609,62)/1091945  Cov:1/1292152  interNum(54/370)
--- GeneralLogic --- trial:11
 1 0 ++ Bas:(17,0,0,0)/1091962  Cov:1/11  interNum(0/0)
--- GeneralLogic --- trial:12
 1 0    Bas:(102,0,0,0)/1092104  Cov:0/40  interNum(0/0)
 2 0    Bas:(659,393,104,49)/1101333  Cov:0/257  interNum(0/0)
 2 1    Bas:(979,619,457,215)/1111289  Cov:0/53027  interNum(42/154)
 3 0    Bas:(4955,4097,621,37)/1481104  Cov:0/5782  interNum(0/0)
 3 1 ++ Bas:(3186,2142,1038,87)/1610525  Cov:1/2393747  interNum(119/698)
--- GeneralLogic --- trial:13
 1 0    Bas:(99,0,0,0)/1610663  Cov:0/30  interNum(0/0)
 2 0    Bas:(645,374,95,47)/1619582  Cov:0/175  interNum(0/0)
 2 1    Bas:(930,570,428,211)/1629010  Cov:0/37957  interNum(41/146)
 3 0    Bas:(4520,3676,550,41)/1970843  Cov:0/2553  interNum(0/0)
 3 1 ++ Bas:(3157,2040,1020,90)/2097474  Cov:1/1840876  interNum(97/649)

Execution time: 38.5seconds.



--- GeneralLogic --- trial:1
 1 0 ++ Bas:(83,0,0,0,0)/83  Cov:1/70  interNum(0/0)
--- GeneralLogic --- trial:2
 1 0    Bas:(103,0,0,0,0)/226  Cov:0/80  interNum(0/0)
 2 0 ++ Bas:(19,19,14,14,0)/3894  Cov:1/99  interNum(0/0)
--- GeneralLogic --- trial:3
 1 0    Bas:(103,0,0,0,0)/4037  Cov:0/72  interNum(0/0)
 2 0 ++ Bas:(36,36,29,29,0)/10078  Cov:1/156  interNum(0/0)
--- GeneralLogic --- trial:4
 1 0    Bas:(103,0,0,0,0)/10221  Cov:0/64  interNum(0/0)
 2 0    Bas:(60,60,53,53,0)/18738  Cov:0/193  interNum(0/0)
 2 1 ++ Bas:(120,106,92,92,0)/21270  Cov:1/13259  interNum(9/20)
--- GeneralLogic --- trial:5
 1 0    Bas:(103,0,0,0,0)/21413  Cov:0/62  interNum(0/0)
 2 0    Bas:(60,60,53,53,0)/29930  Cov:0/193  interNum(0/0)
 2 1    Bas:(519,382,315,315,0)/39963  Cov:0/37540  interNum(25/76)
 3 0 ++ Bas:(184,161,105,104,0)/232225  Cov:1/4953  interNum(0/0)
--- GeneralLogic --- trial:6
 1 0    Bas:(103,0,0,0,0)/232368  Cov:0/58  interNum(0/0)
 2 0    Bas:(61,61,54,54,0)/240896  Cov:0/192  interNum(0/0)
 2 1    Bas:(551,398,327,327,0)/250929  Cov:0/38208  interNum(28/81)
 3 0 ++ Bas:(120,101,66,65,0)/379859  Cov:1/1871  interNum(0/0)
--- GeneralLogic --- trial:7
 1 0    Bas:(103,0,0,0,0)/380002  Cov:0/56  interNum(0/0)
 2 0    Bas:(64,64,57,57,0)/389008  Cov:0/219  interNum(0/0)
 2 1    Bas:(583,419,342,342,0)/399106  Cov:0/37686  interNum(30/88)
 3 0 ++ Bas:(189,159,103,101,0)/612945  Cov:1/2957  interNum(0/0)
--- GeneralLogic --- trial:8
 1 0    Bas:(103,0,0,0,0)/613088  Cov:0/52  interNum(0/0)
 2 0 ++ Bas:(62,62,55,55,0)/621676  Cov:1/213  interNum(0/0)
--- GeneralLogic --- trial:9
 1 0    Bas:(103,0,0,0,0)/621819  Cov:0/48  interNum(0/0)
 2 0    Bas:(61,61,55,55,0)/630999  Cov:0/187  interNum(0/0)
 2 1 ++ Bas:(246,204,166,166,0)/635484  Cov:1/17332  interNum(14/43)
--- GeneralLogic --- trial:10
 1 0    Bas:(103,0,0,0,0)/635627  Cov:0/44  interNum(0/0)
 2 0    Bas:(60,60,54,54,0)/644814  Cov:0/159  interNum(0/0)
 2 1    Bas:(628,413,342,342,0)/654912  Cov:0/29724  interNum(30/89)
 3 0    Bas:(381,260,166,163,0)/1028704  Cov:0/2514  interNum(0/0)
 3 1 ++ Bas:(916,562,328,328,0)/1091945  Cov:1/424649  interNum(22/160)
--- GeneralLogic --- trial:11
 1 0 ++ Bas:(17,0,0,0,0)/1091962  Cov:1/11  interNum(0/0)
--- GeneralLogic --- trial:12
 1 0    Bas:(102,0,0,0,0)/1092104  Cov:0/40  interNum(0/0)
 2 0    Bas:(64,64,56,56,0)/1101333  Cov:0/140  interNum(0/0)
 2 1    Bas:(656,420,342,342,0)/1111289  Cov:0/28481  interNum(30/95)
 3 0    Bas:(377,248,161,157,0)/1481104  Cov:0/1487  interNum(0/0)
 3 1 ++ Bas:(1643,986,550,550,0)/1610525  Cov:1/815460  interNum(51/304)
--- GeneralLogic --- trial:13
 1 0    Bas:(99,0,0,0,0)/1610663  Cov:0/30  interNum(0/0)
 2 0    Bas:(57,57,52,52,0)/1619582  Cov:0/84  interNum(0/0)
 2 1    Bas:(647,399,327,327,0)/1629010  Cov:0/20352  interNum(29/93)
 3 0    Bas:(369,238,158,154,0)/1970843  Cov:0/1071  interNum(0/0)
 3 1 ++ Bas:(1844,1085,601,601,0)/2097474  Cov:1/812196  interNum(53/343)

 Execution time: 17.5 seconds.
#endif