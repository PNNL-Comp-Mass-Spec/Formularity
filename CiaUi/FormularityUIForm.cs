﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;
using System.Threading;
using System.Globalization;

using Support;
using TestFSDBSearch;
using CIA;
using FindChains;

namespace CiaUi {
    public partial class FormularityUIForm : Form {
        public CCia oCCia = new CCia();
        CiaAdvancedForm oCiaAdvancedForm;
        //System.Windows.Forms.CheckBox [] GoldenRuleFilterUsage;
        string [] DBPeaksTableHeaders = new string [] { "Index", "Neutral mass", "Formula", "Error, ppm" };
        public enum EPlotType{ ErrorVsNeutralMass, ErrorVs};
        public FormularityUIForm() {
            InitializeComponent();
            oCiaAdvancedForm = new CiaAdvancedForm( this);
            this.SuspendLayout();
            //================
            //CIA tab
            //================
            //Alignment
            checkBoxAlignment.Checked = oCCia.GetAlignment();
            numericUpDownAlignmentTolerance.Value = ( decimal ) oCCia.GetAlignmentPpmTolerance();

            //Formula assignment
            textBoxDropDB.Text = "Drop DB files";
            if( oCCia.GetDBFilenames().Length > 0 ) {
                textBoxDropDB.AppendText( "\n\rLoaded:" );
                foreach( string Filename in oCCia.GetDBFilenames() ) {
                    textBoxDropDB.AppendText( "\n\r" + Filename );
                }
            }

            //Calibration
            comboBoxCalRegressionModel.DataSource = Enum.GetValues( typeof( TotalCalibration.ttlRegressionType ) );
            comboBoxFormulaScore.DataSource = oCCia.GetFormulaScoreNames();
            comboBoxRelationshipErrorType.DataSource = Enum.GetNames( typeof( CCia.RelationshipErrorType) );
            for ( int Relation = 0; Relation < CCia.RelationBuildingBlockFormulas.Length; Relation++ ) {
                checkedListBoxRelations.Items.Add( oCCia.FormulaToName( CCia.RelationBuildingBlockFormulas [ Relation ] ), oCCia.GetActiveRelationFormulaBuildingBlocks()[ Relation] );
            }

            comboBoxSpecialFilters.Items.Clear();
            string [] SpecialFilterNames = Enum.GetNames( typeof( CCia.ESpecialFilters ) );
            string [] SpecialFilterRules = oCCia.GetSpecialFilterRules();
            for( int SpecialFilter = 0; SpecialFilter < SpecialFilterRules.Length; SpecialFilter++ ) {
                comboBoxSpecialFilters.Items.Add( SpecialFilterNames [ SpecialFilter ] + ": " + SpecialFilterRules [ SpecialFilter ] );
            }

            //================
            //IPA tab (Isotopic pattern algorithm)
            //============
            buttonIpaMergeWithCIA.Visible = false;

            string DefaultParametersFile = Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location ) + "\\DefaultParameters.xml";
            if ( File.Exists( DefaultParametersFile ) == true ) {
                oCCia.LoadParameters( DefaultParametersFile );
            }
            UpdateCiaAndIpaDialogs();
            //===============
            //chartError tab`
            //=============
            comboBoxPlotType.DataSource = Enum.GetNames( typeof( EPlotType ) );
            comboBoxPlotType.SelectedIndex = 0;

            //================
            //DB inspector tab
            //================
            numericDBUpDownMass.Enabled = false;
            tableLayoutPanelDBPeaks.Enabled = false;
            tableLayoutPanelDBPeaks.AutoScroll = true;
            tableLayoutPanelDBPeaks.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            tableLayoutPanelDBPeaks.ColumnStyles.Clear();
            tableLayoutPanelDBPeaks.ColumnCount = DBPeaksTableHeaders.Length;
            for( int iColumn = 0; iColumn < tableLayoutPanelDBPeaks.ColumnCount; iColumn++ ) {
                tableLayoutPanelDBPeaks.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle( SizeType.Percent, ( float ) 100.0 / DBPeaksTableHeaders.Length) );
            }
            tableLayoutPanelDBPeaks.RowStyles.Clear();
            tableLayoutPanelDBPeaks.RowCount = 5 + 1;//Extra Row without RowStyle!!!
            for( int iRow = 0; iRow < tableLayoutPanelDBPeaks.RowCount; iRow++ ) {
                tableLayoutPanelDBPeaks.RowStyles.Add( new System.Windows.Forms.RowStyle( SizeType.Absolute, ( new System.Windows.Forms.TextBox() ).Height + 2 * ( new System.Windows.Forms.TextBox() ).Margin.Top ) );
            }
            for( int iRow = 0; iRow < tableLayoutPanelDBPeaks.RowCount - 1; iRow++ ) {// "-1" Extra Row without Controls!!!
                for( int iColumn = 0; iColumn < tableLayoutPanelDBPeaks.ColumnCount; iColumn++ ) {
                    System.Windows.Forms.TextBox oTextBox = new System.Windows.Forms.TextBox();
                    oTextBox.Anchor = AnchorStyles.None;
                    oTextBox.ReadOnly = true;
                    oTextBox.AutoSize = true;
                    oTextBox.TextAlign = HorizontalAlignment.Center;
                    if( iRow == 0){
                        oTextBox.ReadOnly = true;
                        oTextBox.Text = DBPeaksTableHeaders [ iColumn ];
                    }
                    tableLayoutPanelDBPeaks.Controls.Add( oTextBox, iColumn, iRow );
                }
            }

            //================
            //File convertor tab
            //================
            comboBoxDBAction.DataSource = DBActionMenu;
            comboBoxDBAction.SelectedIndex = 0;
            checkBoxDBCalculateMassFromFormula.Checked = oCCia.GetDBCalculateMassFromFormula();
            checkBoxDBSortByMass.Checked = oCCia.GetDBSort();
            checkBoxDBMassRangePerCsvFile.Checked = oCCia.GetDBMassRangePerCsvFile();
            numericUpDownDBMassRange.Value = (decimal) oCCia.GetDBMassRange();

            //================
            //Filter check tab
            //================

            //================
            //About tab
            //================
            richTextBoxAbout.SelectionFont = new System.Drawing.Font("Microsoft Sans Seri", 10, FontStyle.Bold);
            richTextBoxAbout.SelectedText = "Authors:\n\n";
            richTextBoxAbout.SelectionFont = new System.Drawing.Font("Microsoft Sans Seri", 8, FontStyle.Regular);
            richTextBoxAbout.SelectedText = "Andrey Liyu (Program user interface & CIA conversion from Matlab)";
            richTextBoxAbout.SelectedText = "\rNikola Tolic (Internal calibration, IPA function and DB)";
            richTextBoxAbout.SelectedText = "\rElizabeth Kujawinski & Krista Longnecker (original CIA Matlab code and DB)";
            richTextBoxAbout.SelectedText = "\r\rCompiled: 4/12/2017";


            richTextBoxAbout.SelectionFont = new System.Drawing.Font("Microsoft Sans Seri", 10, FontStyle.Bold);
            richTextBoxAbout.SelectedText = "\r\rDisclaimer:\n";
            richTextBoxAbout.SelectionFont = new System.Drawing.Font("Microsoft Sans Seri", 8, FontStyle.Regular);
            richTextBoxAbout.SelectedText = "This material was prepared as an account of work sponsored by an agency of the United States Government.";
            richTextBoxAbout.SelectedText = "Neither the United States Government nor the United States Department of Energy, nor the Contractor, nor any or their employees, ";
            richTextBoxAbout.SelectedText = "nor any jurisdiction or organization that has cooperated in the development of these materials, ";
            richTextBoxAbout.SelectedText = "makes any warranty, express or implied, or assumes any legal liability or responsibility for the accuracy, ";
            richTextBoxAbout.SelectedText = "completeness, or usefulness or any information, apparatus, product, software, or process disclosed, or represents that its use ";
            richTextBoxAbout.SelectedText = "would not infringe privately owned rights.";
            richTextBoxAbout.SelectedText = "Reference herein to any specific commercial product, process, or service by trade name, trademark,";
            richTextBoxAbout.SelectedText = "manufacturer, or otherwise does not necessarily constitute or imply its endorsement, recommendation, ";
            richTextBoxAbout.SelectedText = "or favoring by the United States Government or any agency thereof, or Battelle Memorial Institute. The ";
            richTextBoxAbout.SelectedText = "views and opinions of authors expressed herein do not necessarily state or reflect those of the United ";
            richTextBoxAbout.SelectedText = "States Government or any agency thereof.";


            richTextBoxAbout.SelectionFont = new System.Drawing.Font("Microsoft Sans Seri", 10, FontStyle.Regular);
            richTextBoxAbout.SelectedText = "\r\r\rPACIFIC NORTHWEST NATIONAL LABORATORY";
            richTextBoxAbout.SelectedText = "\roperated by BATTELLE";
            richTextBoxAbout.SelectedText = "\rfor the UNITED STATES DEPARTMENT OF ENERGY";
            richTextBoxAbout.SelectedText = "\runder Contract DE-AC05-76RL01830";


            //===============
            //Spectra files area
            //===============
            comboBoxIonization.DataSource = Enum.GetValues( typeof( TestFSDBSearch.TotalSupport.IonizationMethod) );
            comboBoxIonization.Text = oCCia.Ipa.Ionization.ToString();
            textBoxAdduct.Text = oCCia.Ipa.Adduct;
            numericUpDownCharge.Value = oCCia.Ipa.CS;

            //checkBoxCIA
            //checkBoxIpa

            this.ResumeLayout();
        }
        private void UpdateCiaAndIpaDialogs() {
            //this.SuspendLayout();
            //input data
            textBoxAdduct.Text = oCCia.Ipa.Adduct;
            comboBoxIonization.Text = oCCia.Ipa.Ionization.ToString();
            numericUpDownCharge.Value = oCCia.Ipa.CS;
            //calibration
            comboBoxCalRegressionModel.Text = oCCia.oTotalCalibration.ttl_cal_regression.ToString();
            numericUpDownCalRelFactor.Value = ( decimal ) oCCia.oTotalCalibration.ttl_cal_rf;
            numericUpDownCalStartTolerance.Value = ( decimal ) oCCia.oTotalCalibration.ttl_cal_start_ppm;
            numericUpDownCalEndTolerance.Value = ( decimal ) oCCia.oTotalCalibration.ttl_cal_target_ppm;
            numericUpDownCalMinSN.Value = ( decimal ) oCCia.oTotalCalibration.ttl_cal_min_sn;
            numericUpDownCalMinRelAbun.Value = ( decimal ) oCCia.oTotalCalibration.ttl_cal_min_abu_pct;
            numericUpDownCalMaxRelAbun.Value = ( decimal ) oCCia.oTotalCalibration.ttl_cal_max_abu_pct;
            //CIA
            checkBoxAlignment.Checked = oCCia.GetAlignment();
            numericUpDownAlignmentTolerance.Value = ( decimal ) oCCia.GetAlignmentPpmTolerance();
            oCiaAdvancedForm.checkBoxCIAAdvAddChains.Checked = oCCia.GetGenerateChainReport();
            oCiaAdvancedForm.numericUpDownCIAAdvMinPeaksPerChain.Value = oCCia.GetMinPeaksPerChain();

            numericUpDownFormulaTolerance.Value = ( decimal ) oCCia.GetFormulaPPMTolerance();
            numericUpDownDBMassLimit.Value = ( decimal ) oCCia.GetMassLimit();
            comboBoxFormulaScore.SelectedIndex = ( int ) oCCia.GetFormulaScore();
            //checkBoxUseCIAFormulaScore.Checked = oCCia.GetUseCIAFormulaScore();
            oCiaAdvancedForm.checkBoxCIAAdvUseKendrick.Checked = oCCia.GetUseKendrick();
            oCiaAdvancedForm.checkBoxCIAAdvUseC13.Checked = oCCia.GetUseC13();
            oCiaAdvancedForm.numericUpDownCIAAdvC13Tolerance.Value = ( decimal ) oCCia.GetC13Tolerance();
            checkBoxUseFormulaFilters.Checked = oCCia.GetUseFormulaFilter();
            oCiaAdvancedForm.checkBoxGoldenRule1.Checked = oCCia.GetGoldenRuleFilterUsage() [ 0 ];//ElementalCounts
            oCiaAdvancedForm.checkBoxGoldenRule2.Checked = oCCia.GetGoldenRuleFilterUsage() [ 1 ];//ValenceRules
            oCiaAdvancedForm.checkBoxGoldenRule3.Checked = oCCia.GetGoldenRuleFilterUsage() [ 2 ];//ElementalRatios
            oCiaAdvancedForm.checkBoxGoldenRule4.Checked = oCCia.GetGoldenRuleFilterUsage() [ 3 ];//HeteroatomCount
            oCiaAdvancedForm.checkBoxGoldenRule5.Checked = oCCia.GetGoldenRuleFilterUsage() [ 4 ];//PositiveAtoms
            oCiaAdvancedForm.checkBoxGoldenRule6.Checked = oCCia.GetGoldenRuleFilterUsage() [ 5 ];//IntegerDBE
            comboBoxSpecialFilters.SelectedIndex = ( int ) oCCia.GetSpecialFilter();
            textBoxUserDefinedFilter.Text = oCCia.GetUserDefinedFilter();

            checkBoxUseRelation.Checked = oCCia.GetUseRelation();
            numericUpDownMaxRelationshipGaps.Value = oCCia.GetMaxRelationGaps();
            numericUpDownRelationErrorValue.Value = ( decimal ) oCCia.GetRelationErrorAMU();
            comboBoxRelationshipErrorType.SelectedIndex = ( int ) oCCia.GetRelationshipErrorType();
            oCiaAdvancedForm.checkBoxCIAAdvBackward.Checked = oCCia.GetUseBackward();
            for ( int BlockIndex = 0; BlockIndex < oCCia.GetActiveRelationFormulaBuildingBlocks().Length; BlockIndex++ ) {
                checkedListBoxRelations.SetItemChecked( BlockIndex, oCCia.GetActiveRelationFormulaBuildingBlocks() [ BlockIndex ] );
            }
            oCiaAdvancedForm.checkBoxIndividualFileReport.Checked = oCCia.GetGenerateIndividualFileReports();
            //checkBoxLogReport.Checked = oCCia.GetLogReportStatus();
            oCiaAdvancedForm.comboBoxOutputFileDelimiter.Text = oCCia.GetOutputFileDelimiterType().ToString();
            oCiaAdvancedForm.comboBoxErrorType.Text = oCCia.GetErrorType().ToString();

            //IPA
            numericUpDownIpaMassTolerance.Value = ( decimal ) oCCia.Ipa.m_ppm_tol;
            numericUpDownIpaMajorPeaksMinSN.Value = ( decimal ) oCCia.Ipa.m_min_major_sn;
            numericUpDownIpaMinorPeaksMinSN.Value = ( decimal ) oCCia.Ipa.m_min_minor_sn;

            numericUpDownIpaMinMajorPeaksToAbsToReport.Value = ( decimal ) oCCia.Ipa.m_min_major_pa_mm_abs_2_report;
            checkBoxIpaMatchedPeakReport.Checked = oCCia.Ipa.m_matched_peaks_report;

            numericUpDownIpaMinPeakProbabilityToScore.Value = ( decimal ) oCCia.Ipa.m_min_p_to_score;
            textBoxIpaFilter.Text = oCCia.Ipa.m_IPDB_ec_filter;

            buttonIpaMergeWithCIA.Visible = false;

            //this.ResumeLayout();
        }
        private void CIAUIForm_FormClosing( object sender, FormClosingEventArgs e ) {
            string DefaultParametersFile = Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location ) + "\\DefaultParameters.xml";
            oCCia.SaveParameters( DefaultParametersFile );
        }

        //Input files
        private void textBoxAdduct_KeyDown( object sender, KeyEventArgs e ) {
            try {
                if( e.KeyCode == Keys.Return ) {
                    oCCia.Ipa.Adduct = textBoxAdduct.Text;
                    textBoxResult.Text = oCCia.Ipa.ChargedMassFormula_Descriptive;
                    //numericUpDownMass_ValueChanged( sender, e );//to update DB instector tab
                }
            } catch( Exception ex ) {
                MessageBox.Show( ex.Message );
            }
        }
        private void textBoxAdduct_Leave( object sender, EventArgs e ) {
            try {
                oCCia.Ipa.Adduct = textBoxAdduct.Text;
                textBoxResult.Text = oCCia.Ipa.ChargedMassFormula_Descriptive;
                //numericUpDownMass_ValueChanged( sender, e );//to update DB instector tab
            } catch( Exception ex ) {
                MessageBox.Show( ex.Message );
            }
        }
        private void comboBoxIonization_SelectedIndexChanged( object sender, EventArgs e ) {
            oCCia.Ipa.Ionization = ( TestFSDBSearch.TotalSupport.IonizationMethod ) Enum.Parse( typeof( TestFSDBSearch.TotalSupport.IonizationMethod), comboBoxIonization.Text );
            textBoxResult.Text = oCCia.Ipa.ChargedMassFormula_Descriptive;
            //numericUpDownMass_ValueChanged( sender, e );//to update DB instector tab
        }
        private void numericUpDownCharge_ValueChanged( object sender, EventArgs e ) {
            oCCia.Ipa.CS = ( int ) Math.Abs( numericUpDownCharge.Value );
            textBoxResult.Text = oCCia.Ipa.ChargedMassFormula_Descriptive;
            //numericUpDownMass_ValueChanged( sender, e );//to update DB instector tab
        }
        void CheckToProcess() {
            bool CalibrationReady = ( ( ( TotalCalibration.ttlRegressionType ) comboBoxCalRegressionModel.SelectedValue == TotalCalibration.ttlRegressionType.none )
                        | ( ( ( TotalCalibration.ttlRegressionType ) comboBoxCalRegressionModel.SelectedValue != TotalCalibration.ttlRegressionType.none ) & ( textBoxCalFile.TextLength > "Drop calibration file: ".Length ) ) );

            bool CIAReady = ( oCCia.GetDBFilenames().Length > 0 ) & CalibrationReady;
            checkBoxCIA.Enabled = CIAReady;

            bool IpaReady = oCCia.Ipa.IPDB_Ready & CalibrationReady;
            checkBoxIpa.Enabled = IpaReady;

            if( ( CIAReady == true ) && ( checkBoxCIA.Checked == true )
                    || ( IpaReady == true ) && ( checkBoxIpa.Checked == true ) ) {
                textBoxDropSpectraFiles.BackColor = Color.LightGreen;
                textBoxDropSpectraFiles.Enabled = true;
            } else {
                textBoxDropSpectraFiles.BackColor = SystemColors.ControlLight;
                textBoxDropSpectraFiles.Enabled = false;
            }

            bool ChainCalibrationReady = ( ( TotalCalibration.ttlRegressionType ) comboBoxCalRegressionModel.SelectedValue != TotalCalibration.ttlRegressionType.none )
                    & ( textBoxCalFile.TextLength > "Drop calibration file: ".Length );

            if ( ChainCalibrationReady == true ){
                textBoxChainDropSpectraFile.BackColor = Color.LightGreen;
                textBoxChainDropSpectraFile.Enabled = true;
            } else {
                textBoxChainDropSpectraFile.BackColor = SystemColors.ControlLight;
                textBoxChainDropSpectraFile.Enabled = false;
            }
        }
        private void checkBoxCIA_CheckedChanged( object sender, EventArgs e ) {
            CheckToProcess();
        }
        private void checkBoxIpa_CheckedChanged( object sender, EventArgs e ) {
            CheckToProcess();
        }
        private void textBoxCalFile_DragEnter( object sender, DragEventArgs e ) {
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) == true ) {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void textBoxCalFile_DragDrop( object sender, DragEventArgs e ) {
            string [] Filenames = ( string [] ) e.Data.GetData( DataFormats.FileDrop );
            oCCia.oTotalCalibration.Load( Filenames [ 0 ] );
            textBoxCalFile.Text = "Drop calibration file: " + Path.GetFileName( Filenames [ 0 ] );
            CheckToProcess();
        }
        private void comboBoxCalRegressionModel_SelectedIndexChanged( object sender, EventArgs e ) {
            if( comboBoxCalRegressionModel.Text == TotalCalibration.ttlRegressionType.none.ToString() ) {
                textBoxCalFile.Enabled = false;
                numericUpDownCalRelFactor.Enabled = false;
                numericUpDownCalStartTolerance.Enabled = false;
                numericUpDownCalEndTolerance.Enabled = false;
                numericUpDownCalMinSN.Enabled = false;
                numericUpDownCalMinRelAbun.Enabled = false;
                numericUpDownCalMaxRelAbun.Enabled = false;
            } else {
                textBoxCalFile.Enabled = true;
                numericUpDownCalRelFactor.Enabled = true;
                numericUpDownCalStartTolerance.Enabled = true;
                numericUpDownCalEndTolerance.Enabled = true;
                numericUpDownCalMinSN.Enabled = true;
                numericUpDownCalMinRelAbun.Enabled = true;
                numericUpDownCalMaxRelAbun.Enabled = true;
            }
            CheckToProcess();
        }
        private void textBoxDropSpectraFiles_DragEnter( object sender, DragEventArgs e ) {
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) == true ) {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void textBoxDropSpectraFiles_DragDrop( object sender, DragEventArgs e ) {
            textBoxDropSpectraFiles.BackColor = Color.Red;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture( "ja-JP" );
            try {
                string [] Filenames = ( string [] ) e.Data.GetData( DataFormats.FileDrop );
                //log file
                string LogFileName = DateTime.Now.ToString();
                LogFileName = Path.GetDirectoryName( Filenames [ 0 ] ) + "\\" + "Report" + LogFileName.Replace( "/", "" ).Replace( ":", "" ).Replace( " ", "" ) + ".log";
                StreamWriter oStreamLogWriter = new StreamWriter( LogFileName );

                int FileCount = Filenames.Length;
                double [] [] Masses = new double [ FileCount ] [];
                double [] [] Abundances = new double [ FileCount ] [];
                double [] [] SNs = new double [ FileCount ] [];
                double [] [] Resolutions = new double [ FileCount ] [];
                double [] [] RelAbundances = new double [ FileCount ] [];

                //Read files & Calibration
                oCCia.Ipa.Adduct = textBoxAdduct.Text;
                oCCia.Ipa.Ionization = ( TestFSDBSearch.TotalSupport.IonizationMethod ) Enum.Parse( typeof( TestFSDBSearch.TotalSupport.IonizationMethod ), comboBoxIonization.Text );
                oCCia.Ipa.CS = ( int ) Math.Abs( numericUpDownCharge.Value );

                oCCia.oTotalCalibration.ttl_cal_regression = ( TotalCalibration.ttlRegressionType ) Enum.Parse( typeof( TotalCalibration.ttlRegressionType ), comboBoxCalRegressionModel.Text );
                oCCia.oTotalCalibration.ttl_cal_rf = ( double ) numericUpDownCalRelFactor.Value;
                oCCia.oTotalCalibration.ttl_cal_start_ppm = ( double ) numericUpDownCalStartTolerance.Value;
                oCCia.oTotalCalibration.ttl_cal_target_ppm = ( double ) numericUpDownCalEndTolerance.Value;
                oCCia.oTotalCalibration.ttl_cal_min_sn = ( double ) numericUpDownCalMinSN.Value;
                oCCia.oTotalCalibration.ttl_cal_min_abu_pct = ( double ) numericUpDownCalMinRelAbun.Value;
                oCCia.oTotalCalibration.ttl_cal_max_abu_pct = ( double ) numericUpDownCalMaxRelAbun.Value;
                double [] MaxAbundances = new double [ FileCount ];
                double [] [] CalMasses = new double [ FileCount ] [];
                for( int FileIndex = 0; FileIndex < FileCount; FileIndex++ ) {
                    //read files
                    Support.CFileReader.ReadFile( Filenames [ FileIndex ], out Masses [ FileIndex ], out Abundances [ FileIndex ], out SNs [ FileIndex ], out Resolutions [ FileIndex ], out RelAbundances [ FileIndex ] );
                    //MaxAbundance = Abundances [ FileIndex ] [ 0 ];
                    //foreach( double Abundabce in Abundances [ FileIndex ] ) { if( MaxAbundance < Abundabce ) { MaxAbundance = Abundabce; } }
                    //MaxAbundances [ FileIndex ] = MaxAbundance;
                    MaxAbundances [ FileIndex ] = Support.CArrayMath.Max( Abundances [ FileIndex ] );
                    //Calibration
                    if( oCCia.oTotalCalibration.ttl_cal_regression == TotalCalibration.ttlRegressionType.none ) {
                        CalMasses [ FileIndex ] = new double [ Masses [ FileIndex ].Length ];
                        for( int PeakIndex = 0; PeakIndex < CalMasses.Length; PeakIndex++ ) {
                            CalMasses [ PeakIndex ] = Masses [ PeakIndex ];
                        }
                    } else {
                        oCCia.oTotalCalibration.cal_log.Clear();
                        double MaxAbundance = Support.CArrayMath.Max( Abundances [ FileIndex ] );
                        CalMasses [ FileIndex ] = oCCia.oTotalCalibration.ttl_LQ_InternalCalibration( ref Masses [ FileIndex ], ref Abundances [ FileIndex ], ref SNs [ FileIndex ], MaxAbundances [ FileIndex ] );
                        oStreamLogWriter.WriteLine();
                        oStreamLogWriter.WriteLine( "Calibration of " + Path.GetFileName( Filenames [ FileIndex ] ) );
                        oStreamLogWriter.WriteLine();
                        oStreamLogWriter.Write( oCCia.oTotalCalibration.cal_log );
                    }
                }
                if( checkBoxCIA.Checked == true ) {
                    //Alignment
                    oCCia.SetAlignment( checkBoxAlignment.Checked );
                    oCCia.SetAlignmentPpmTolerance( ( double ) numericUpDownAlignmentTolerance.Value );
                    oCCia.SetAddChains( false);

                    //Formula assignment
                    oCCia.SetMassLimit( ( double ) numericUpDownDBMassLimit.Value );
                    oCCia.SetFormulaScore( ( CCia.EFormulaScore ) Array.IndexOf( oCCia.GetFormulaScoreNames(), comboBoxFormulaScore.Text ) );
                    if( checkBoxCIAUseDefault.Checked == false ) {
                        oCCia.SetUseKendrick( oCiaAdvancedForm.checkBoxCIAAdvUseKendrick.Checked );
                        oCCia.SetUseC13( oCiaAdvancedForm.checkBoxCIAAdvUseC13.Checked );
                        oCCia.SetC13Tolerance( ( double ) oCiaAdvancedForm.numericUpDownCIAAdvC13Tolerance.Value );
                    } else {
                        oCCia.SetUseKendrick( true);
                        oCCia.SetUseC13( true);
                        oCCia.SetC13Tolerance( ( double ) numericUpDownFormulaTolerance.Value );
                    }

                    //Filters
                    oCCia.SetUseFormulaFilter( checkBoxUseFormulaFilters.Checked );
                    bool [] GoldenFilters = new bool [ oCCia.GoldenRuleFilterNames.Length ];
                    if( checkBoxCIAUseDefault.Checked == false ) {
                        GoldenFilters [ 0 ] = oCiaAdvancedForm.checkBoxGoldenRule1.Checked;
                        GoldenFilters [ 1 ] = oCiaAdvancedForm.checkBoxGoldenRule2.Checked;
                        GoldenFilters [ 2 ] = oCiaAdvancedForm.checkBoxGoldenRule3.Checked;
                        GoldenFilters [ 3 ] = oCiaAdvancedForm.checkBoxGoldenRule4.Checked;
                        GoldenFilters [ 4 ] = oCiaAdvancedForm.checkBoxGoldenRule5.Checked;
                        GoldenFilters [ 5 ] = oCiaAdvancedForm.checkBoxGoldenRule6.Checked;
                    } else {
                        GoldenFilters [ 0 ] = true;
                        GoldenFilters [ 1 ] = true;
                        GoldenFilters [ 2 ] = true;
                        GoldenFilters [ 3 ] = true;
                        GoldenFilters [ 4 ] = true;
                        GoldenFilters [ 5 ] = false;
                    }
                    oCCia.SetGoldenRuleFilterUsage( GoldenFilters );

                    oCCia.SetSpecialFilter( ( CCia.ESpecialFilters ) Enum.Parse( typeof( CCia.ESpecialFilters ), comboBoxSpecialFilters.Text.Split( new char [] { ':' } ) [ 0 ] ) );
                    oCCia.SetUserDefinedFilter( textBoxUserDefinedFilter.Text );
                    //Relationships
                    oCCia.SetUseRelation( checkBoxUseRelation.Checked );
                    oCCia.SetMaxRelationGaps( ( int ) numericUpDownMaxRelationshipGaps.Value );
                    oCCia.SetRelationshipErrorType( ( CCia.RelationshipErrorType ) Enum.Parse( typeof( CCia.RelationshipErrorType ), comboBoxRelationshipErrorType.Text ) );
                    oCCia.SetRelationErrorAMU( ( double ) numericUpDownRelationErrorValue.Value );
                    if( checkBoxCIAUseDefault.Checked == false ) {
                        oCCia.SetUseBackward( oCiaAdvancedForm.checkBoxCIAAdvBackward.Checked );
                    } else {
                        oCCia.SetUseBackward( false );
                    }

                    //short [] [] ActiveRelationBlocks = new short [ checkedListBoxRelations.CheckedItems.Count ] [];
                    //for( int ActiveFormula = 0; ActiveFormula < checkedListBoxRelations.CheckedItems.Count; ActiveFormula++ ) {
                    //    ActiveRelationBlocks [ ActiveFormula ] = oCCia.NameToFormula( checkedListBoxRelations.CheckedItems [ ActiveFormula ].ToString() );
                    //}
                    //oCCia.SetRelationFormulaBuildingBlocks( ActiveRelationBlocks );
                    bool [] ActiveRelationBlocks = new bool [ CCia.RelationBuildingBlockFormulas.Length ];
                    for ( int ActiveFormula = 0; ActiveFormula < ActiveRelationBlocks.Length; ActiveFormula++ ) {
                        ActiveRelationBlocks [ ActiveFormula ] = checkedListBoxRelations.GetItemChecked( ActiveFormula);
                    }
                    oCCia.SetActiveRelationFormulaBuildingBlocks( ActiveRelationBlocks );

                    if( checkBoxCIAUseDefault.Checked == false ) {
                        //Reports
                        oCCia.SetGenerateIndividualFileReports( oCiaAdvancedForm.checkBoxIndividualFileReport.Checked );
                        oCCia.SetAddChains( oCiaAdvancedForm.checkBoxCIAAdvAddChains.Checked );
                        //File formats
                        oCCia.SetOutputFileDelimiterType( ( CCia.EDelimiters ) Enum.Parse( typeof( CCia.EDelimiters ), oCiaAdvancedForm.comboBoxOutputFileDelimiter.Text ) );
                        oCCia.SetErrorType( ( CCia.EErrorType ) Enum.Parse( typeof( CCia.EErrorType ), oCiaAdvancedForm.comboBoxErrorType.Text ) );
                    } else {
                        //Reports
                        if( checkBoxAlignment.Checked == true ) {
                            oCCia.SetGenerateIndividualFileReports( false );
                        } else {
                            oCCia.SetGenerateIndividualFileReports( true);
                        }
                        oCCia.SetAddChains( false);
                        //File formats
                        oCCia.SetOutputFileDelimiterType( CCia.EDelimiters.Comma);
                        oCCia.SetErrorType( CCia.EErrorType.Signed);
                    }

                    //Process
                    oCCia.Process( Filenames, Masses, Abundances, SNs, Resolutions, RelAbundances, CalMasses, oStreamLogWriter );

                    //change textbox
                    textBoxDropSpectraFiles.Text = "Drop Spectra Files";
                    textBoxDropSpectraFiles.AppendText( "\r\nProcessed files:" );
                    foreach( string Filename in Filenames ) {
                        textBoxDropSpectraFiles.AppendText( "\r\n" + Path.GetFileName( Filename ) );
                    }
                }
                if( checkBoxIpa.Checked == true ) {
                    bool b = oCCia.Ipa.SetCalculation();

                    oCCia.Ipa.m_ppm_tol = ( double ) numericUpDownIpaMassTolerance.Value;
                    oCCia.Ipa.m_min_major_sn = ( double ) numericUpDownIpaMajorPeaksMinSN.Value;
                    oCCia.Ipa.m_min_minor_sn = ( double ) numericUpDownIpaMinorPeaksMinSN.Value;

                    oCCia.Ipa.m_min_major_pa_mm_abs_2_report = ( double ) numericUpDownIpaMinMajorPeaksToAbsToReport.Value;
                    oCCia.Ipa.m_matched_peaks_report = checkBoxIpaMatchedPeakReport.Checked;

                    oCCia.Ipa.m_min_p_to_score = ( double ) numericUpDownIpaMinPeakProbabilityToScore.Value;

                    oCCia.Ipa.m_IPDB_ec_filter = textBoxIpaFilter.Text;

                    for( int FileIndex = 0; FileIndex < FileCount; FileIndex++ ) {
                        oCCia.Ipa.IPDB_log.Clear();
                        oCCia.Ipa.ttlSearch( ref CalMasses [ FileIndex ], ref Abundances [ FileIndex ], ref SNs [ FileIndex ], ref MaxAbundances [ FileIndex ], Filenames [ FileIndex ] );
                        oStreamLogWriter.Write( oCCia.Ipa.IPDB_log );
                    }
                }
                oStreamLogWriter.Flush();
                oStreamLogWriter.Close();
            } catch( Exception Ex ) {
                MessageBox.Show( Ex.Message );
                textBoxDropSpectraFiles.BackColor = Color.Pink;
            }
            textBoxDropSpectraFiles.BackColor = Color.LightGreen;
        }

        //tab
        private void tabControl1_SelectedIndexChanged( object sender, EventArgs e ) {
            string ddd = tabControl1.TabPages [ tabControl1.SelectedIndex ].Text;
            if( ddd == "CIA DB inspector" ) {
                numericUpDownMass_ValueChanged( sender, e );
            }
        }
        //CIA tab
        private void checkBoxAlignment_CheckedChanged( object sender, EventArgs e ) {
            numericUpDownAlignmentTolerance.Enabled = checkBoxAlignment.Checked;
        }
        private void textBoxDropDB_DragEnter( object sender, DragEventArgs e ) {
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) == true ) {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void textBoxDropDB_DragDrop( object sender, DragEventArgs e ) {
            string [] Filenames = ( string [] ) e.Data.GetData( DataFormats.FileDrop );
            oCCia.LoadDBs( Filenames );
            textBoxDropDB.Text = "Drop DB files";
            textBoxDropDB.AppendText( "\r\nLoaded:" );
            foreach( string Filename in oCCia.GetDBFilenames() ) {
                textBoxDropDB.AppendText( "\r\n" + Path.GetFileName( Filename ) );
            }
            numericDBUpDownMass.Enabled = true;
            textBoxDBRecords.Text = oCCia.GetDBRecords().ToString();
            textBoxDBMinMass.Text = oCCia.GetDBMinMass().ToString();
            textBoxDBMaxMass.Text = oCCia.GetDBMaxMass().ToString();
            textBoxDBMinError.Text = oCCia.GetDBMinError().ToString();
            textBoxDBMaxError.Text = oCCia.GetDBMaxError().ToString();
            CheckToProcess();
        }
        private void numericUpDownFormulaError_ValueChanged( object sender, EventArgs e ) {
            oCCia.SetFormulaPPMTolerance( ( double ) numericUpDownFormulaTolerance.Value );
            numericUpDownMass_ValueChanged( sender, e );//to update DB instector tab
        }
        private void checkBoxUseFormulaFilters_CheckedChanged( object sender, EventArgs e ) {
            //groupBoxGoldenRuleFilters.Enabled = checkBoxUseFormulaFilters.Checked;
            comboBoxSpecialFilters.Enabled = checkBoxUseFormulaFilters.Checked;
            textBoxUserDefinedFilter.Enabled = checkBoxUseFormulaFilters.Checked;
        }
        private void checkBoxUseRelation_CheckedChanged( object sender, EventArgs e ) {
            numericUpDownMaxRelationshipGaps.Enabled = checkBoxUseRelation.Checked;
            comboBoxRelationshipErrorType.Enabled = checkBoxUseRelation.Checked;
            numericUpDownRelationErrorValue.Enabled = checkBoxUseRelation.Checked;
            checkedListBoxRelations.Enabled = checkBoxUseRelation.Checked;
        }
        private void buttonLoadCiaParameters_Click( object sender, EventArgs e ) {
            textBoxAdduct.Text = "H";
            comboBoxIonization.Text = TestFSDBSearch.TotalSupport.IonizationMethod.proton_attachment.ToString();
            numericUpDownCharge.Value = 1;

            checkBoxAlignment.Checked = true;
            numericUpDownAlignmentTolerance.Value = ( decimal ) oCCia.GetAlignmentPpmTolerance();

            checkBoxUseRelation.Checked = true;
            numericUpDownDBMassLimit.Value = 500;
            comboBoxFormulaScore.SelectedIndex = ( int) CCia.EFormulaScore.HAcap;

            checkBoxUseFormulaFilters.Checked = true;
            //for( int GoldenRuleFilter = 0; GoldenRuleFilter < GoldenRuleFilterUsage.Length - 1; GoldenRuleFilter++ ) {
            //    GoldenRuleFilterUsage [ GoldenRuleFilter ].Checked = true;
            //}
            //GoldenRuleFilterUsage [ 5].Checked = false;
            comboBoxSpecialFilters.SelectedIndex = 0;
            textBoxUserDefinedFilter.Text = string.Empty;

            checkBoxUseRelation.Checked = true;
            numericUpDownMaxRelationshipGaps.Value = 5;
            numericUpDownRelationErrorValue.Value = ( decimal ) 0.00002;
            for( int RelationBlock = 0; RelationBlock < checkedListBoxRelations.Items.Count; RelationBlock++ ) {
                if( ( RelationBlock == 0 ) || ( RelationBlock == 2 ) || ( RelationBlock == 6 ) ) {
                    checkedListBoxRelations.SetItemChecked( RelationBlock, true );
                } else {
                    checkedListBoxRelations.SetItemChecked( RelationBlock, false );
                }
            }

            oCiaAdvancedForm.checkBoxIndividualFileReport.Checked = false;
            oCiaAdvancedForm.checkBoxCIAAdvAddChains.Checked = false;
            oCiaAdvancedForm.numericUpDownCIAAdvMinPeaksPerChain.Value = 3;
            oCiaAdvancedForm.comboBoxErrorType.Text = CCia.EErrorType.CIA.ToString();
        }

        private void buttonSwitchToAdvanced_Click( object sender, EventArgs e ) {
            oCiaAdvancedForm.numericUpDownCharge.Value = numericUpDownCharge.Value;
            oCiaAdvancedForm.textBoxAdduct.Text = textBoxAdduct.Text;
            oCiaAdvancedForm.comboBoxIonization.Text = comboBoxIonization.Text;
            oCiaAdvancedForm.textBoxResult.Text = textBoxResult.Text;

            oCiaAdvancedForm.textBoxCalFile.Text = textBoxCalFile.Text;
            oCiaAdvancedForm.comboBoxCalRegressionModel.SelectedIndex = comboBoxCalRegressionModel.SelectedIndex;
            oCiaAdvancedForm.numericUpDownCalStartTolerance.Value = numericUpDownCalStartTolerance.Value;
            oCiaAdvancedForm.numericUpDownCalRelFactor.Value = numericUpDownCalRelFactor.Value;
            oCiaAdvancedForm.numericUpDownCalEndTolerance.Value = numericUpDownCalEndTolerance.Value;
            oCiaAdvancedForm.numericUpDownCalMinSN.Value = numericUpDownCalMinSN.Value;
            oCiaAdvancedForm.numericUpDownCalMinRelAbun.Value = numericUpDownCalMinRelAbun.Value;
            oCiaAdvancedForm.numericUpDownCalMaxRelAbun.Value = numericUpDownCalMaxRelAbun.Value;

            oCiaAdvancedForm.checkBoxAlignment.Checked = checkBoxAlignment.Checked;
            oCiaAdvancedForm.numericUpDownAlignmentTolerance.Value = numericUpDownAlignmentTolerance.Value;
            oCiaAdvancedForm.textBoxDropDB.Text = textBoxDropDB.Text;
            oCiaAdvancedForm.numericUpDownFormulaTolerance.Value = numericUpDownFormulaTolerance.Value;
            oCiaAdvancedForm.numericUpDownDBMassLimit.Value = numericUpDownDBMassLimit.Value;
            oCiaAdvancedForm.comboBoxFormulaScore.SelectedIndex = comboBoxFormulaScore.SelectedIndex;
            oCiaAdvancedForm.checkBoxUseFormulaFilters.Checked = checkBoxUseFormulaFilters.Checked;
            oCiaAdvancedForm.checkBoxUseRelation.Checked = checkBoxUseRelation.Checked;
            oCiaAdvancedForm.numericUpDownMaxRelationshipGaps.Value = numericUpDownMaxRelationshipGaps.Value;
            oCiaAdvancedForm.comboBoxRelationshipErrorType.SelectedIndex = comboBoxRelationshipErrorType.SelectedIndex;
            oCiaAdvancedForm.numericUpDownRelationErrorValue.Value = numericUpDownRelationErrorValue.Value;

            for( int RelationIndex = 0; RelationIndex < checkedListBoxRelations.CheckedItems.Count; RelationIndex++ ) {
                oCiaAdvancedForm.checkedListBoxRelations.SetItemChecked( RelationIndex, checkedListBoxRelations.GetItemChecked( RelationIndex ) );
            }

            oCiaAdvancedForm.comboBoxSpecialFilters.SelectedIndex = comboBoxSpecialFilters.SelectedIndex;
            oCiaAdvancedForm.textBoxUserDefinedFilter.Text = textBoxUserDefinedFilter.Text;
            oCiaAdvancedForm.CheckToProcess();

            this.Visible = false;
            DialogResult sss = oCiaAdvancedForm.ShowDialog( this );

            numericUpDownCharge.Value = oCiaAdvancedForm.numericUpDownCharge.Value;
            textBoxAdduct.Text = oCiaAdvancedForm.textBoxAdduct.Text;
            comboBoxIonization.Text = oCiaAdvancedForm.comboBoxIonization.Text;
            textBoxResult.Text = oCiaAdvancedForm.textBoxResult.Text;

            textBoxCalFile.Text = oCiaAdvancedForm.textBoxCalFile.Text;
            comboBoxCalRegressionModel.SelectedIndex = oCiaAdvancedForm.comboBoxCalRegressionModel.SelectedIndex;
            numericUpDownCalStartTolerance.Value = oCiaAdvancedForm.numericUpDownCalStartTolerance.Value;
            numericUpDownCalRelFactor.Value = oCiaAdvancedForm.numericUpDownCalRelFactor.Value;
            numericUpDownCalEndTolerance.Value = oCiaAdvancedForm.numericUpDownCalEndTolerance.Value;
            numericUpDownCalMinSN.Value = oCiaAdvancedForm.numericUpDownCalMinSN.Value;
            numericUpDownCalMinRelAbun.Value = oCiaAdvancedForm.numericUpDownCalMinRelAbun.Value;
            numericUpDownCalMaxRelAbun.Value = oCiaAdvancedForm.numericUpDownCalMaxRelAbun.Value;

            checkBoxAlignment.Checked = oCiaAdvancedForm.checkBoxAlignment.Checked;
            numericUpDownAlignmentTolerance.Value = oCiaAdvancedForm.numericUpDownAlignmentTolerance.Value;
            textBoxDropDB.Text = oCiaAdvancedForm.textBoxDropDB.Text;
            numericUpDownFormulaTolerance.Value = oCiaAdvancedForm.numericUpDownFormulaTolerance.Value;
            numericUpDownDBMassLimit.Value = oCiaAdvancedForm.numericUpDownDBMassLimit.Value;
            comboBoxFormulaScore.SelectedIndex = oCiaAdvancedForm.comboBoxFormulaScore.SelectedIndex;
            checkBoxUseFormulaFilters.Checked = oCiaAdvancedForm.checkBoxUseFormulaFilters.Checked;
            checkBoxUseRelation.Checked = oCiaAdvancedForm.checkBoxUseRelation.Checked;
            numericUpDownMaxRelationshipGaps.Value = oCiaAdvancedForm.numericUpDownMaxRelationshipGaps.Value;
            comboBoxRelationshipErrorType.SelectedIndex = oCiaAdvancedForm.comboBoxRelationshipErrorType.SelectedIndex;
            numericUpDownRelationErrorValue.Value = oCiaAdvancedForm.numericUpDownRelationErrorValue.Value;

            for( int RelationIndex = 0; RelationIndex < checkedListBoxRelations.CheckedItems.Count; RelationIndex++ ) {
                checkedListBoxRelations.SetItemChecked( RelationIndex, oCiaAdvancedForm.checkedListBoxRelations.GetItemChecked( RelationIndex ) );
            }

            comboBoxSpecialFilters.SelectedIndex = oCiaAdvancedForm.comboBoxSpecialFilters.SelectedIndex;
            textBoxUserDefinedFilter.Text = oCiaAdvancedForm.textBoxUserDefinedFilter.Text;
            CheckToProcess();

            this.Visible = true;
        }

        //Ipa tab
        private void textBoxIpaDropDBFile_DragEnter( object sender, DragEventArgs e ) {
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) == true ) {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void textBoxIpaDropDBFile_DragDrop( object sender, DragEventArgs e ) {
            string [] Filenames = ( string [] ) e.Data.GetData( DataFormats.FileDrop );
            oCCia.Ipa.LoadTabulatedDB( Filenames [ 0 ] );//???
            CheckToProcess();
            textBoxIpaDropDBFile.Text = Filenames [ 0 ];
        }

        //Error plot tab
        List<double> XData = new List<double>();
        List<double> YData = new List<double>();
        private void chartError_DragEnter( object sender, DragEventArgs e ) {
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) == true ) {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void chartError_DragDrop( object sender, DragEventArgs e ) {
            string [] Filenames = ( string [] ) e.Data.GetData( DataFormats.FileDrop );
            string [] Lines = File.ReadAllLines( Filenames[ 0]);
            string FileName = Path.GetFileNameWithoutExtension( Filenames [ 0 ] );
            string [] Headers = Lines [ 0].Split( new char [] { ',' } );
            int XAxisColumnIndex = -1;
            int YAxisColumnIndex = -1;
            for( int Column = 0; Column < Headers.Length; Column++ ) {
                if( Headers [ Column ] == textBoxXAxisColumnHeader.Text ) { XAxisColumnIndex = Column; }
                if( Headers [ Column ] == textBoxYAxisColumnHeader.Text ) { YAxisColumnIndex = Column; }
                if( ( XAxisColumnIndex != -1 ) && ( YAxisColumnIndex != -1 ) ) {
                    break;
                }
            }
            if( ( XAxisColumnIndex != -1 ) == false ) {
                MessageBox.Show( "There is not " + textBoxXAxisColumnHeader.Text + " column header");
                return;
            }
            if( ( YAxisColumnIndex != -1 ) == false ) {
                MessageBox.Show( "There is not " + textBoxYAxisColumnHeader.Text + " column header" );
                return;
            }

            for( int Line = 1; Line < Lines.Length; Line++ ) {
                string [] Words = Lines [ Line ].Split( new char [] { ',' } );
                if( Words [ YAxisColumnIndex ] == "0" ) { continue; }
                XData.Add( double.Parse( Words [ XAxisColumnIndex ] ) );
                YData.Add( double.Parse( Words [ YAxisColumnIndex ] ) );
            }

            chartError.Series [ 0 ].Name = string.Empty;//SeriesName;
            chartError.Series [ 0 ].Points.Clear();
            double XMin;
            double XMax;
            double YMin;
            double YMax;

            switch( (EPlotType) Enum.Parse( typeof( EPlotType), comboBoxPlotType.Text) ){
                case EPlotType.ErrorVsNeutralMass:
                    chartError.Series [ 0 ].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
                    chartError.ChartAreas [ 0 ].AxisX.Title = "Neutral mass, Da";
                    chartError.ChartAreas [ 0 ].AxisY.Title = "Error, ppm";
                    YMin = YData [ 0];
                    YMax = YMin;
                    for( int Point = 0; Point < XData.Count;  Point++ ) {
                        chartError.Series [ 0 ].Points.AddXY( XData [ Point ], YData [ Point ] );
                        if( YMin > YData [ Point ] ) { YMin = YData [ Point ]; }
                        if( YMax < YData [ Point ] ) { YMax = YData [ Point ]; }
                    }
                    XMin = XData [ 0 ];
                    XMax = XData [ XData.Count - 1 ];
                    break;
                case EPlotType.ErrorVs:
                    chartError.Series [ 0 ].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
                    chartError.ChartAreas [ 0 ].AxisX.Title = "Error, ppm";
                    chartError.ChartAreas [ 0 ].AxisY.Title = "Counts";
                    int BinCount = (int) Math.Ceiling( Math.Sqrt( XData.Count ) );
                    XMin = YData[ 0];
                    XMax = XMin;
                    for( int Index = 1; Index < YData.Count; Index++ ) {
                        if( XMin > YData [ Index ] ) { XMin = YData [ Index ]; }
                        if( XMax < YData [ Index ] ) { XMax = YData [ Index ]; }
                    }
                    double BinSize = ( XMax - XMin ) / BinCount;
                    int [] Bins = new int [ BinCount];
                    YMin = 0;
                    YMax = 0;
                    foreach( double Y in YData ) {
                        int BinIndex = (int) Math.Floor( ( Y - XMin ) / BinSize );
                        if( BinIndex >= BinCount ) { BinIndex = BinCount - 1; }
                        Bins [ BinIndex ]++;
                        if( YMax < Bins [ BinIndex ] ) { YMax = Bins [ BinIndex ]; }
                    }
                    for( int Point = 0; Point < Bins.Length; Point++ ) {
                        double XValue = XMin + BinSize * ( Point + 0.5 );
                        chartError.Series [ 0 ].Points.AddXY( XValue, Bins [ Point ] );
                    }
                    break;
                default:
                    return;
            }
            chartError.ChartAreas [ 0 ].AxisX.Interval = ( XMax - XMin ) / 5;
            chartError.ChartAreas [ 0 ].AxisY.Interval = ( YMax - YMin ) / 5;
            chartError.ChartAreas [ 0 ].AxisX.LabelStyle.Format = "0.#e-0";
            chartError.ChartAreas [ 0 ].AxisY.LabelStyle.Format = "0.#e-0";
        }

        //DB tools tab
        private void numericUpDownMass_ValueChanged( object sender, EventArgs e ) {
            //textBoxResult.Text = oCCia.Ipa.ChargedMassFormula_Descriptive;
            if( numericDBUpDownMass.Value < 0 ) { return; }
            tableLayoutPanelDBPeaks.SuspendLayout();
            tableLayoutPanelDBPeaks.Enabled = true;
            double Mass = (double) numericDBUpDownMass.Value;
            double NeutralMass = oCCia.Ipa.GetNeutralMass( Mass );
            textBoxDBNeutralMass.Text = NeutralMass.ToString();
            double Error = CPpmError.PpmToError( NeutralMass, oCCia.GetFormulaPPMTolerance() );
            textBoxDBNeutralMassPlusError.Text = ( NeutralMass + Error ).ToString();
            textBoxDBNeutralMassMinusError.Text = ( NeutralMass - Error ).ToString();
            int LowerIndex, UpperIndex;
            int Records;
            if( oCCia.GetDBLimitIndexes( NeutralMass, out LowerIndex, out UpperIndex ) == false ) {
                Records = 0;
            } else {
                Records = UpperIndex - LowerIndex + 1;
            }
            int Rows = Records + 2;//+ Head + Last Row without Controls
            textBoxDBRecordsInErrorRange.Text = Records.ToString();
            if( tableLayoutPanelDBPeaks.RowStyles.Count > Rows) {
                for( int Row = tableLayoutPanelDBPeaks.RowCount - 1; Row >= Rows; Row-- ) {
                    for( int iColumn = 0; iColumn < tableLayoutPanelDBPeaks.ColumnCount; iColumn++ ) {
                        tableLayoutPanelDBPeaks.Controls.RemoveAt( tableLayoutPanelDBPeaks.Controls.Count - 1);
                    }
                    tableLayoutPanelDBPeaks.RowStyles.RemoveAt( Row);
                }
                tableLayoutPanelDBPeaks.RowCount = Rows;
            } else if( tableLayoutPanelDBPeaks.RowStyles.Count < Rows ) {
                tableLayoutPanelDBPeaks.RowCount = Rows;
                for( int Row = tableLayoutPanelDBPeaks.RowStyles.Count - 1; Row < Rows - 1; Row++ ) {//"Count-1" due Last Row without Controls
                    tableLayoutPanelDBPeaks.RowStyles.Add( new System.Windows.Forms.RowStyle( SizeType.Absolute, ( new System.Windows.Forms.TextBox() ).Height + 2 * ( new System.Windows.Forms.TextBox() ).Margin.Top ) );
                    for( int iColumn = 0; iColumn < tableLayoutPanelDBPeaks.ColumnCount; iColumn++ ) {
                        System.Windows.Forms.TextBox oTextBox = new System.Windows.Forms.TextBox();
                        oTextBox.Anchor = AnchorStyles.None;
                        oTextBox.ReadOnly = true;
                        oTextBox.AutoSize = true;
                        oTextBox.TextAlign = HorizontalAlignment.Center;
                        tableLayoutPanelDBPeaks.Controls.Add( oTextBox, iColumn, Row );
                    }
                }
            }
            for( int Row = 1; Row < Rows - 1; Row++ ) {
                int DBIndex = LowerIndex + Row - 1;
                tableLayoutPanelDBPeaks.GetControlFromPosition( 0, Row ).Text = DBIndex.ToString();
                tableLayoutPanelDBPeaks.GetControlFromPosition( 1, Row ).Text = oCCia.GetDBMass( DBIndex ).ToString();
                tableLayoutPanelDBPeaks.GetControlFromPosition( 2, Row ).Text = oCCia.GetDBFormulaName( DBIndex );
                tableLayoutPanelDBPeaks.GetControlFromPosition( 3, Row ).Text = CPpmError.SignedMassErrorPPM( NeutralMass, oCCia.GetDBMass( DBIndex ) ).ToString( "E" );
            }
            tableLayoutPanelDBPeaks.ResumeLayout();
        }
        private void textBoxDBDropFiles_DragEnter( object sender, DragEventArgs e ) {
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) == true ) {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void textBoxDBDropFiles_DragDrop( object sender, DragEventArgs e ) {
            try {
                if( oCCia.GetDBFilenames().Length == 0 ) { throw new Exception( "Drop DB file." ); }
                string [] Filenames = ( string [] ) e.Data.GetData( DataFormats.FileDrop );
                //???oCCia.ReadFiles( Filenames);
                //oCCia.ReportFormulas();
            } catch {
            }
        }

        //File convertor tab
        string [] InputFileTextFormats = { ".txt", ".csv", ".xls", ".xlsx" };
        string [] DBActionMenu = {
            "One ASCII -> one binary",
            "Many ASCIIs -> one binary",
            "Binary -> CSV"};
        private void comboBoxDBAction_SelectedIndexChanged( object sender, EventArgs e ) {
            if( comboBoxDBAction.Text == DBActionMenu [ 0 ] ) {
                checkBoxDBCalculateMassFromFormula.Enabled = true;
                checkBoxDBSortByMass.Enabled = true;
                checkBoxDBMassRangePerCsvFile.Enabled = false;
            } else if( comboBoxDBAction.Text == DBActionMenu [ 1 ] ) {
                checkBoxDBCalculateMassFromFormula.Enabled = true;
                checkBoxDBSortByMass.Enabled = true;
                checkBoxDBMassRangePerCsvFile.Enabled = false;
            } else if( comboBoxDBAction.Text == DBActionMenu [ 2 ] ) {
                checkBoxDBCalculateMassFromFormula.Enabled = false;
                checkBoxDBSortByMass.Enabled = false;
                checkBoxDBMassRangePerCsvFile.Enabled = true;
            }
        }
        private void checkBoxDBMassRangePerCsvFile_CheckedChanged( object sender, EventArgs e ) {
            numericUpDownDBMassRange.Enabled = checkBoxDBMassRangePerCsvFile.Enabled;
        }
        private void textBoxConvertDBs_DragEnter( object sender, DragEventArgs e ) {
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) == true ) {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void textBoxConvertDBs_DragDrop( object sender, DragEventArgs e ) {
            string [] Filenames = ( string [] ) e.Data.GetData( DataFormats.FileDrop );
            oCCia.SetDBCalculateMassFromFormula( checkBoxDBCalculateMassFromFormula.Checked);
            oCCia.SetDBSort( checkBoxDBSortByMass.Checked);
            oCCia.SetDBMassRangePerCsvFile( checkBoxDBMassRangePerCsvFile.Checked);
            oCCia.SetDBMassRange( ( double) numericUpDownDBMassRange.Value);
            if( comboBoxDBAction.Text == DBActionMenu [ 0 ] ) {
                foreach( string Filename in Filenames ) {
                    if( InputFileTextFormats.Contains( Path.GetExtension( Filename ) ) == true ) {
                        oCCia.DBConvertAsciiToBin( Filename );
                    } else {
                        MessageBox.Show( "Extention of file (" + Path.GetFileName( Filename ) + ") is not ASCII" );
                    }
                }
            } else if( comboBoxDBAction.Text == DBActionMenu [ 1 ] ) {
                bool ErrorTypeExtention = false;
                foreach( string Filename in Filenames ) {
                    if( InputFileTextFormats.Contains( Path.GetExtension( Filename ) ) != true ) {
                        MessageBox.Show( "Extention of file (" + Path.GetFileName( Filename ) + ") is not ASCII" );
                        ErrorTypeExtention = true;
                    }
                }
                if( ErrorTypeExtention == false ) {
                    oCCia.DBConvertAsciisToBin( Filenames );
                }
            } else if( comboBoxDBAction.Text == DBActionMenu [ 2 ] ) {
                foreach( string Filename in Filenames ) {
                    if( Path.GetExtension( Filename ) == ".bin" ) {
                        oCCia.DBConvertBinToCsv( Filename);
                    } else {
                        MessageBox.Show( "Extention of file (" + Path.GetFileName( Filename ) + ") is not bin" );
                    }
                }
            }
        }
        private void textBoxCompareReports_DragEnter( object sender, DragEventArgs e ) {
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) == true ) {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void textBoxCompareReports_DragDrop( object sender, DragEventArgs e ) {
            string [] Filenames = ( string [] ) e.Data.GetData( DataFormats.FileDrop );
            CompareReports( Filenames );
        }
        void CompareReports( string [] Filenames ) {
            int TotalSamples = 0;
            List<string> SampleNames = new List<string>();
            foreach( string Filename in Filenames ) {
                string [] ColumnHeaders = File.ReadAllLines( Filename ) [ 0 ].Split( oCCia.WordSeparators);
                int Samples = ColumnHeaders.Length - 10;
                TotalSamples = TotalSamples + Samples;
                for( int Sample = 0; Sample < Samples; Sample++ ) {
                    SampleNames.Add( ColumnHeaders [ 10 + Sample ] );
                }
            }
            SortedDictionary<double, ReportData> FormulaDict = new SortedDictionary<double, ReportData>();
            int StartSample = 0;
            foreach( string Filename in Filenames ) {
                string [] LineString = File.ReadAllLines( Filename );
                string [] ColumnHeaders = File.ReadAllLines( Filename ) [ 0 ].Split( oCCia.WordSeparators);
                int Samples = ColumnHeaders.Length - 10;
                for( int Line = 1; Line < LineString.Length; Line++ ) {
                    string [] LineParts = LineString[ Line].Split( oCCia.WordSeparators);
                    short [] Formula = new short[ CCia.ElementCount];
                    for( int Element = 0; Element < CCia.ElementCount; Element++ ) {
                        Formula [ Element ] = Int16.Parse( LineParts [ 2 + Element ] );
                    }
                    if( oCCia.IsFormula( Formula ) == false ) {
                        continue;
                    }
                    double NeutralMass = oCCia.FormulaToNeutralMass( Formula );
                    double [] Abundances;
                    if( FormulaDict.ContainsKey( NeutralMass ) == false ) {
                        ReportData Data = new ReportData();
                        Data.Formula = ( short [] ) Formula.Clone();
                        Data.Abundances = new double [ TotalSamples ];
                        FormulaDict.Add( NeutralMass, Data );
                        Abundances = Data.Abundances;
                    } else {
                        Abundances = FormulaDict [ NeutralMass ].Abundances;
                    }
                    for( int Sample = 0; Sample < Samples; Sample++ ) {
                        Abundances [ StartSample + Sample ] = double.Parse( LineParts [ 10 + Sample ] );
                    }
                }
                StartSample = StartSample + Samples;
            }
            string Delimiter = ",";
            string HeaderLine = "NeutralMass" + Delimiter + "Mass";
            foreach( string Element in Enum.GetNames( typeof( CCia.EElemIndex ) ) ) {
                HeaderLine = HeaderLine + Delimiter + Element;
            }
            foreach( string SampleName in SampleNames ) {
                HeaderLine = HeaderLine + Delimiter + SampleName;
            }
            StreamWriter oStreamWriter = new StreamWriter( Path.GetDirectoryName( Filenames [ 0 ] ) + "\\Comparision.csv" );
            oStreamWriter.WriteLine( HeaderLine );

            foreach( KeyValuePair<double, ReportData> KVP in FormulaDict ) {
                string Line = KVP.Key.ToString() + Delimiter + oCCia.Ipa.GetChargedMass( KVP.Key ).ToString();
                foreach( short Count in KVP.Value.Formula ) {
                    Line = Line + Delimiter + Count.ToString();
                }
                foreach( double Abundance in KVP.Value.Abundances ) {
                    Line = Line + Delimiter + Abundance.ToString();
                }
                oStreamWriter.WriteLine( Line );
            }
            oStreamWriter.Close();
        }
        private void textBoxConverFiles_DragEnter( object sender, DragEventArgs e ) {
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) == true ) {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void textBoxConverFiles_DragDrop( object sender, DragEventArgs e ) {
            string [] Filenames = ( string [] ) e.Data.GetData( DataFormats.FileDrop );
            foreach( string Filename in Filenames ) {
                XmlToXls( Filename );
            }
        }
        private void XmlToXls( string Filename ) {
            XmlDocument XmlDoc = new XmlDocument();
            XmlDoc.Load( Filename);
            //check Bruker instrument
            XmlNodeList Nodes = XmlDoc.GetElementsByTagName( "fileinfo" );
            if( Nodes.Count != 1 ) { return; }
            if( Nodes [ 0 ].Attributes [ "appname" ].Value != "Bruker Compass DataAnalysis" ) { return; }
            //read peaks
            XmlNodeList MsPeakNodes = XmlDoc.GetElementsByTagName( "ms_peaks" );
            if( MsPeakNodes.Count != 1 ) { return; }
            XmlNode MsPeakNode = MsPeakNodes [ 0 ];
            int Peaks = MsPeakNode.ChildNodes.Count;
            int [] myLengthsArray = new int [ 2 ] { Peaks, 5 };
            int [] myBoundsArray = new int [ 2 ] { 1, 1 };
            Array myArray = Array.CreateInstance( typeof( double ), myLengthsArray, myBoundsArray );
            double Maxi = 0;
            for( int Peak = 1; Peak <= Peaks; Peak++ ) {
                //<pk res="930674.5" algo="FTMS" fwhm="0.000218" a="102.53" sn="7.15" i="646225.1" mz="203.034719"/>
                XmlAttributeCollection Attributes = MsPeakNode.ChildNodes [ Peak - 1].Attributes;
                myArray.SetValue( double.Parse( Attributes [ "mz" ].Value), Peak, 1 );
                myArray.SetValue( double.Parse( Attributes [ "i" ].Value), Peak, 2 );
                myArray.SetValue( double.Parse( Attributes [ "sn" ].Value), Peak, 3 );
                myArray.SetValue( double.Parse( Attributes [ "res" ].Value), Peak, 4 );
                double Currenti = (double) myArray.GetValue( Peak, 2 );
                if( Maxi < Currenti ) { Maxi = Currenti; }

            }
            XmlDoc = null;
            for ( int Peak = 1; Peak <= Peaks; Peak++ ){
                double rel_ab = ( (double) myArray.GetValue( Peak, 2 ) ) / Maxi;
                myArray.SetValue( rel_ab, Peak, 5 );
            }

            Microsoft.Office.Interop.Excel.Application MyApp = new Microsoft.Office.Interop.Excel.Application();
            MyApp.Visible = false;
            Microsoft.Office.Interop.Excel.Workbook MyBook = MyApp.Workbooks.Add( 1);
            Microsoft.Office.Interop.Excel.Worksheet MySheet = (Microsoft.Office.Interop.Excel.Worksheet)MyBook.Sheets [ 1 ];
            ((Microsoft.Office.Interop.Excel.Range)MySheet.Cells [ 1, 1]).Value = "mz";
            ((Microsoft.Office.Interop.Excel.Range)MySheet.Cells [ 1, 2]).Value = "i";
            ((Microsoft.Office.Interop.Excel.Range)MySheet.Cells [ 1, 3]).Value = "sn";
            ((Microsoft.Office.Interop.Excel.Range)MySheet.Cells [ 1, 4]).Value = "res";
            ((Microsoft.Office.Interop.Excel.Range)MySheet.Cells [ 1, 5]).Value = "rel_ab";

            Microsoft.Office.Interop.Excel.Range MyRange = MySheet.get_Range( "A2", "E" + Peaks.ToString() ) ;
            MyRange.Value = myArray;
            MyBook.SaveAs( Filename.Substring( 0, Filename.Length - Path.GetExtension( Filename).Length ) + ".xls" );
            string sss = Path.GetFileNameWithoutExtension( Filename );

            oCCia.CleanComObject( MyRange );
            MyRange = null;
            oCCia.CleanComObject( MySheet );
            MySheet = null;
            MyBook.Close( null, null, null );
            oCCia.CleanComObject( MyBook );
            MyBook = null;
            MyApp.Quit();
            oCCia.CleanComObject( MyApp );
            MyApp = null;
            GC.Collect();
        }

        //filter check tab
        private void buttonFilterCheckFormula_Click( object sender, EventArgs e ) {
            try {
                System.Data.DataTable UserDefinedFilter = new System.Data.DataTable();
                UserDefinedFilter.Columns.Add( "Mass", typeof( double ) );
                foreach ( string Name in Enum.GetNames( typeof( CCia.EElemIndex ) ) ) {
                    UserDefinedFilter.Columns.Add( Name, typeof( short ) );
                }
                UserDefinedFilter.Columns.Add( "UserDefinedFilter", typeof( bool ), textBoxFilter.Text );
                UserDefinedFilter.Rows.Add( UserDefinedFilter.NewRow() );

                double Mass = ( ( int ) numericUpDownCAtoms.Value ) * CElements.C
                        + ( ( int ) numericUpDownHAtoms.Value ) * CElements.H
                        + ( ( int ) numericUpDownOAtoms.Value ) * CElements.O
                        + ( ( int ) numericUpDownNAtoms.Value ) * CElements.N
                        + ( ( int ) numericUpDownSAtoms.Value ) * CElements.S
                        + ( ( int ) numericUpDownPAtoms.Value ) * CElements.P
                        + ( ( int ) numericUpDownNaAtoms.Value ) * CElements.Na;
                textBoxNeutralMass.Text = Mass.ToString();

                UserDefinedFilter.Rows [ 0 ] [ "Mass" ] = Mass;
                UserDefinedFilter.Rows [ 0 ] [ CCia.EElemIndex.C.ToString() ] = numericUpDownCAtoms.Value;
                UserDefinedFilter.Rows [ 0 ] [ CCia.EElemIndex.H.ToString() ] = numericUpDownHAtoms.Value;
                UserDefinedFilter.Rows [ 0 ] [ CCia.EElemIndex.O.ToString() ] = numericUpDownOAtoms.Value;
                UserDefinedFilter.Rows [ 0 ] [ CCia.EElemIndex.N.ToString() ] = numericUpDownNAtoms.Value;
                UserDefinedFilter.Rows [ 0 ] [ CCia.EElemIndex.S.ToString() ] = numericUpDownSAtoms.Value;
                UserDefinedFilter.Rows [ 0 ] [ CCia.EElemIndex.P.ToString() ] = numericUpDownPAtoms.Value;
                UserDefinedFilter.Rows [ 0 ] [ CCia.EElemIndex.Na.ToString() ] = numericUpDownNaAtoms.Value;

                textBoxFilterResult.Text = ( ( bool ) UserDefinedFilter.Rows [ 0 ] [ "UserDefinedFilter" ] ).ToString();
            } catch ( Exception ex ) {
                textBoxFilterResult.Text = "Error: " + ex.Message;
            }
        }
        string [] DBCompositions = new string []{
                "C", "H", "N", "O", "P", "S", "CH", "CN", "CO", "CP",
                "CS", "HN", "HO", "HP", "HS", "NO", "NP", "NS", "OP", "OS",
                "PS", "CHN", "CHO", "CHP", "CHS", "CNO", "CNP", "CNS", "COP", "COS",
                "CPS", "HNO", "HNP", "HNS", "HOP", "HOS", "HPS", "NOP", "NOS", "NPS",
                "OPS", "CHNO", "CHNP", "CHNS", "CHOP", "CHOS", "CHPS", "CNOP", "CNOS", "CNPS",
                "COPS", "HNOP", "HNOS", "HNPS", "HOPS", "NOPS", "CHNOP", "CHNOS", "CHNPS", "CHOPS",
                "CNOPS", "HNOPS", "CHNOPS"
        };
        private void buttonFilterCheckDB_Click( object sender, EventArgs e ) {
            if( oCCia.DBFormulas == null){
                return;
            }
            System.Data.DataTable UserDefinedFilter = new System.Data.DataTable();
            UserDefinedFilter.Columns.Add( "Mass", typeof( double ) );
            foreach ( string Name in Enum.GetNames( typeof( CCia.EElemIndex ) ) ) {
                UserDefinedFilter.Columns.Add( Name, typeof( short ) );
            }
            UserDefinedFilter.Columns.Add( "UserDefinedFilter", typeof( bool ), textBoxFilter.Text );
            UserDefinedFilter.Rows.Add( UserDefinedFilter.NewRow() );

            int [] CompositionCounts = new int [ DBCompositions.Length ];
            for ( int DBFormulaIndex = 0; DBFormulaIndex < oCCia.DBFormulas.Length; DBFormulaIndex++ ) {
                UserDefinedFilter.Rows [ 0 ] [ "Mass" ] = oCCia.DBMasses [ DBFormulaIndex ];
                short [] CurFormula = oCCia.DBFormulas [ DBFormulaIndex ];
                for ( int Element = 0; Element < CCia.ElementCount; Element++ ) {
                    UserDefinedFilter.Rows [ 0 ] [ Enum.GetName( typeof( CCia.EElemIndex ), Element ) ] = CurFormula [ Element ];
                }
                if ( ( bool ) UserDefinedFilter.Rows [ 0 ] [ "UserDefinedFilter" ] == false ) {
                    continue;
                }

                bool bC = ( CurFormula [ ( int ) CCia.EElemIndex.C ] > 0 ) | ( CurFormula [ ( int ) CCia.EElemIndex.C13 ] > 0 );
                bool bH = CurFormula [ ( int ) CCia.EElemIndex.H ] > 0;
                bool bO = CurFormula [ ( int ) CCia.EElemIndex.O ] > 0;
                bool bN = CurFormula [ ( int ) CCia.EElemIndex.N ] > 0;
                bool bS = CurFormula [ ( int ) CCia.EElemIndex.S ] > 0;
                bool bP = CurFormula [ ( int ) CCia.EElemIndex.P ] > 0;
                bool bNa = CurFormula [ ( int ) CCia.EElemIndex.Na ] > 0;

                for ( int CompositionIndex = 0; CompositionIndex < DBCompositions.Length; CompositionIndex++ ) {
                    string Composition = DBCompositions [ CompositionIndex ];
                    if ( Composition.Contains( "C" ) == true ) { if ( bC == false ) { continue; } }
                    if ( Composition.Contains( "H" ) == true ) { if ( bH == false ) { continue; } }
                    if ( Composition.Contains( "O" ) == true ) { if ( bO == false ) { continue; } }
                    if ( Composition.Contains( "N" ) == true ) { if ( bN == false ) { continue; } }
                    if ( Composition.Contains( "S" ) == true ) { if ( bS == false ) { continue; } }
                    if ( Composition.Contains( "P" ) == true ) { if ( bP == false ) { continue; } }
                    if ( Composition.Contains( "Na" ) == true ) { if ( bNa == false ) { continue; } }
                    CompositionCounts [ CompositionIndex ]++;
                }
            }
            string [] Lines = new string [ DBCompositions.Length + 2];
            Lines [ 0 ] = "Total DB formulas," + oCCia.DBFormulas.Length;
            Lines [ 1 ] = "Filter," + textBoxFilter.Text;
            for ( int DBFormulaIndex = 0; DBFormulaIndex < DBCompositions.Length; DBFormulaIndex++ ) {
                Lines [ DBFormulaIndex + 2] = DBCompositions [ DBFormulaIndex ] + "," + CompositionCounts [ DBFormulaIndex ];
            }
            File.WriteAllLines( System.IO.Path.GetDirectoryName( System.Windows.Forms.Application.ExecutablePath ) + "\\DBFilteredCompositions.csv", Lines);
        }
        //chain tab
        private void textBoxDropSpectraFile_DragEnter( object sender, DragEventArgs e ) {
            if ( e.Data.GetDataPresent( DataFormats.FileDrop ) == true ) {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void textBoxDropSpectraFile_DragDrop( object sender, DragEventArgs e ) {
            textBoxChainDropSpectraFile.BackColor = Color.Red;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture( "ja-JP" );
            try {
                string [] Filenames = ( string [] ) e.Data.GetData( DataFormats.FileDrop );
                string OutputFilename = Path.GetDirectoryName( Filenames [ 0 ] ) + "\\" + Path.GetFileNameWithoutExtension( Filenames [ 0 ] );
                int FileCount = Filenames.Length;
                //Read files & Calibration
                oCCia.Ipa.Adduct = textBoxAdduct.Text;
                oCCia.Ipa.Ionization = ( TestFSDBSearch.TotalSupport.IonizationMethod ) Enum.Parse( typeof( TestFSDBSearch.TotalSupport.IonizationMethod ), comboBoxIonization.Text );
                oCCia.Ipa.CS = ( int ) Math.Abs( numericUpDownCharge.Value );

                oCCia.oTotalCalibration.ttl_cal_regression = ( TotalCalibration.ttlRegressionType ) Enum.Parse( typeof( TotalCalibration.ttlRegressionType ), comboBoxCalRegressionModel.Text );
                oCCia.oTotalCalibration.ttl_cal_rf = ( double ) numericUpDownCalRelFactor.Value;
                oCCia.oTotalCalibration.ttl_cal_start_ppm = ( double ) numericUpDownCalStartTolerance.Value;
                oCCia.oTotalCalibration.ttl_cal_target_ppm = ( double ) numericUpDownCalEndTolerance.Value;
                oCCia.oTotalCalibration.ttl_cal_min_sn = ( double ) numericUpDownCalMinSN.Value;
                oCCia.oTotalCalibration.ttl_cal_min_abu_pct = ( double ) numericUpDownCalMinRelAbun.Value;
                oCCia.oTotalCalibration.ttl_cal_max_abu_pct = ( double ) numericUpDownCalMaxRelAbun.Value;

                int ChainMinPeaks = (int) numericUpDownChainMinPeaks.Value;
                double PeakPpmError = ( double ) numericUpDownChainPpmError.Value;

                Support.InputData Data = new Support.InputData();
                Support.CFileReader.ReadFile( Filenames [ 0 ], out Data );
                oCCia.oTotalCalibration.cal_log.Clear();
                double [] CalMasses = oCCia.oTotalCalibration.ttl_LQ_InternalCalibration( ref Data.Masses, ref Data.Abundances, ref Data.S2Ns, Support.CArrayMath.Max( Data.Abundances) );
                double MinMass = 0;
                double MaxMass = Data.Masses[ Data.Masses.Length - 1];

                //non-calibrated
                CChainBlocks oCChainBlocks = new CChainBlocks();
                oCChainBlocks.FindChains( Data, ChainMinPeaks, PeakPpmError, PeakPpmError, MaxMass, MinMass, MaxMass, false );
                int ChainsCount = Data.Chains.Length;
                textBoxChainRawNoncal.Text = ChainsCount.ToString();
                int MaxChainsPeakIndex = oCChainBlocks.GetMaxChainsPeak( Data );
                textBoxChainRawNoncalMaxChainsPeakIndex.Text = MaxChainsPeakIndex.ToString();
                textBoxChainRawNoncalMaxChainsPeakMass.Text = Data.Masses[ MaxChainsPeakIndex].ToString( "F6");
                if ( ( checkBoxChainNoncalOutput.Checked == true) && ( checkBoxChainRawChainOutput.Checked == true )){
                    if ( checkBoxChainChainOutput.Checked == true ) {
                        oCChainBlocks.ChainsToFile( Data, OutputFilename + "NoncalRawChains.csv" );
                    }
                    if ( checkBoxChainChainsPerPeakOutput.Checked == true ) {
                        oCChainBlocks.PeakChainsToFile( Data, OutputFilename + "NoncalRawPeakChains.csv" );
                    }
                }
                oCChainBlocks.CreateUniqueChains( Data, PeakPpmError );
                ChainsCount = Data.Chains.Length;
                textBoxChainUniqueNoncal.Text = ChainsCount.ToString();
                MaxChainsPeakIndex = oCChainBlocks.GetMaxChainsPeak( Data );
                textBoxChainUniqueNoncalMaxChainsPeakIndex.Text = MaxChainsPeakIndex.ToString();
                textBoxChainUniqueNoncalMaxChainsPeakMass.Text = Data.Masses [ MaxChainsPeakIndex ].ToString( "F6" );
                if ( ( checkBoxChainNoncalOutput.Checked == true)
                        && ( checkBoxChainUniqueChainOutput.Checked == true ) ){
                    if ( checkBoxChainChainOutput.Checked == true ) {
                        oCChainBlocks.ChainsToFile( Data, OutputFilename + "NoncalIniqueChains.csv" );
                    }
                    if ( checkBoxChainChainsPerPeakOutput.Checked == true ) {
                        oCChainBlocks.PeakChainsToFile( Data, OutputFilename + "NoncalUniquePeakChains.csv" );
                    }
                }
                //calibrated
                Data.Masses = CalMasses;
                oCChainBlocks.FindChains( Data, ChainMinPeaks, PeakPpmError, PeakPpmError, MaxMass, MinMass, MaxMass, false );
                ChainsCount = Data.Chains.Length;
                textBoxChainRawCal.Text = ChainsCount.ToString();
                MaxChainsPeakIndex = oCChainBlocks.GetMaxChainsPeak( Data );
                textBoxChainRawCalMaxChainsPeakIndex.Text = MaxChainsPeakIndex.ToString();
                textBoxChainRawCalMaxChainsPeakMass.Text = Data.Masses [ MaxChainsPeakIndex ].ToString( "F6" );
                if ( ( checkBoxChainCalOutput.Checked == true )
                        && ( checkBoxChainRawChainOutput.Checked == true ) ){
                    if(checkBoxChainChainOutput.Checked == true){
                        oCChainBlocks.ChainsToFile( Data, OutputFilename + "CalRawChains.csv" );
                    }
                    if ( checkBoxChainChainsPerPeakOutput.Checked == true ) {
                        oCChainBlocks.PeakChainsToFile( Data, OutputFilename + "CalRawPeakChains.csv" );
                    }
                }
                oCChainBlocks.CreateUniqueChains( Data, PeakPpmError );
                int UniqueCalChains = Data.Chains.Length;
                textBoxChainUniqueCal.Text = UniqueCalChains.ToString();
                MaxChainsPeakIndex = oCChainBlocks.GetMaxChainsPeak( Data );
                textBoxChainUniqueCalMaxChainsPeakIndex.Text = MaxChainsPeakIndex.ToString();
                textBoxChainUniqueCalMaxChainsPeakMass.Text = Data.Masses [ MaxChainsPeakIndex ].ToString( "F6" );
                if ( ( checkBoxChainCalOutput.Checked == true )
                        && ( checkBoxChainUniqueChainOutput.Checked == true ) ){
                    if ( checkBoxChainChainOutput.Checked == true ) {
                        oCChainBlocks.ChainsToFile( Data, OutputFilename + "CalUniqueChains.csv" );
                    }
                    if ( checkBoxChainChainsPerPeakOutput.Checked == true ) {
                        oCChainBlocks.PeakChainsToFile( Data, OutputFilename + "CalUniquePeakChains.csv" );
                    }
                }

                textBoxChainRawResult.Text = ( Convert.ToInt32( textBoxChainRawNoncal.Text) - Convert.ToInt32( textBoxChainRawCal.Text) ).ToString();
                textBoxChainUniqueResult.Text = ( Convert.ToInt32( textBoxChainUniqueNoncal.Text ) - Convert.ToInt32( textBoxChainUniqueCal.Text ) ).ToString();
                textBoxChainDropSpectraFile.BackColor = Color.LightGreen;
             } catch( Exception Ex ) {
                MessageBox.Show( Ex.Message );
                textBoxChainDropSpectraFile.BackColor = Color.Pink;
            }
        }

        //Save/restore parameters
        private void buttonSaveParameters_Click( object sender, EventArgs e ) {
            SaveFileDialog OSD = new SaveFileDialog();
            OSD.Title = "Save parameters";
            OSD.InitialDirectory = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location );
            OSD.Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*";
            OSD.FilterIndex = 1;
            if( OSD.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
                oCCia.SaveParameters( OSD.FileName);
            }
        }
        private void buttonLoadParameters_Click( object sender, EventArgs e ) {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Title = "Load parameters";
            OFD.InitialDirectory = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location );
            OFD.Filter = "XML Files (.xml)|*.xml|All Files (*.*)|*.*";
            OFD.FilterIndex = 1;
            OFD.Multiselect = false;
            if( OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK){
                oCCia.LoadParameters( OFD.FileName );
                UpdateCiaAndIpaDialogs();
            }

        }
    }
    class ReportData {
        public short [] Formula;
        public double [] Abundances;
    }
}
