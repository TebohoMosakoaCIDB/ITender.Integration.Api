namespace ITender.Integration.Api.Infrastructure.Mappers
{
    public static class TenderSchema
    {
        // Identity
        public const string EntityLogicalName = "nv_tender";
        public const string Id = "nv_tenderid";
        public const string Name = "nv_name";
        public const string EmployerTenderNumber = "nv_employertendernumber";
        public const string CidbReferenceNumber = "nv_cidbreferencenumber";

        // Notice details
        public const string TenderTitle = "nv_tendertitle";
        public const string TendersInvitedFor = "nv_tendersinvitedfor";
        public const string PreferencesOffered = "nv_preferencesoffered";
        public const string EligibilityCriteria = "nv_eligibilitycriteria";

        // Location
        public const string ProvinceId = "nv_provinceid";
        public const string MetroDistrictMunicipalityId = "nv_metrodistrictmunicipalityid";
        public const string LocalMunicipalityId = "nv_localmunicipalityid";
        // TODO: Latitude, Longitude, GPS Coordinate Type — not visible in the screenshots.
        //       Confirm actual logical names before using.

        // Contract details
        public const string TypeOfContract = "nv_typeofcontract";
        public const string ClassOfConstructionWorksId = "nv_classofconstructionworkid";
        public const string ClassOfConstructionWorksSubCategoryId = "nv_classofconstructionworkssubcategory";
        public const string ClassOfConstructionWorksAltId = "nv_classofconstructionworksaltid";
        public const string TenderValueRange = "nv_tendervaluerange";
        public const string EmergingEnterpriseSupport = "nv_emergingenterprisesupport";
        public const string NameOfTargetedDevelopmentProgramme = "nv_nameoftargeteddevelopmentprogramme";
        public const string NationalContractorDevelopmentProgramme = "nv_nationalcontractordevelopmentprogramme";
        public const string TermContract = "nv_termcontract";

        // Employer
        public const string EmployerId = "nv_employer_tender_id";

        // Documents & payment
        public const string DateAndTimeDocumentsAvailableFrom = "nv_dateandtimedocumentsavailablefrom";
        public const string DepositAmount = "nv_depositamount";
        public const string AdditionalPaymentAndCollectionDetails = "nv_additionalpaymentandcollectiondetails";
        public const string MethodOfPaymentBankGuaranteedCheque = "new_methodofpaymentbankguaranteedcheque";
        public const string MethodOfPaymentCash = "new_methodofpaymentcash";
        public const string MethodOfPaymentProofOfDeposit = "new_methodofpaymentproofofdeposit";
        public const string AdditionalCollectionAddressFlag = "new_additionalcollectionaddress";

        // Address 1 (primary)
        public const string Address1Line1 = "nv_address1_line1";
        public const string Address1Line2 = "nv_address1_line2";
        public const string Address1Line3 = "nv_address1_line3";
        public const string Address1Line4 = "nv_address1_line4";

        // Address 2 (additional collection)
        public const string Address2Line1 = "nv_address2_line1";
        public const string Address2Line2 = "nv_address2_line2";
        public const string Address2Line3 = "nv_address2_line3";
        public const string Address2Line4 = "nv_address2_line4";

        // Clarification meeting
        public const string ClarificationMeeting = "nv_clarificationmeeting";
        public const string ClarificationMeetingCompulsory = "nv_clarificationmeetingcompulsory";
        public const string ClarificationMeetingDateAndTime = "nv_clarificationmeetingdateandtime";
        public const string ClarificationMeetingPlace = "nv_clarificationmeetingplace";
        public const string AddClarificationMeeting = "nv_addclarificationmeeting";
        public const string AddClarificationMeetingCompulsory = "nv_addclarificationmeetingcompulsory";
        public const string AddClarificationMeetingDateAndTime = "nv_addclarificationmeetingdateandtime";
        public const string AddClarificationMeetingPlace = "nv_addclarificationmeetingplace";

        // Submission
        public const string ClosingDateAndTime = "nv_closingdateandtime";
        public const string NotAcceptedEmail = "nv_notacceptedemail";
        public const string NotAcceptedFacsimile = "nv_notacceptedfacsimile";
        public const string NotAcceptedTelegraphic = "nv_notacceptedtelegraphic";
        public const string NotAcceptedTelephonic = "nv_notacceptedtelephonic";
        public const string NotAcceptedTelex = "nv_notacceptedtelex";

        // Status
        public const string StateCode = "statecode";
        public const string StatusCode = "statuscode";
        public const string IsClosed = "nv_isclosed";
        public const string IsEOIorTender = "nv_iseoiortender";
        public const string ContractPartOfEPWP = "nv_contractpartofepwp";
        public const string IsEmergencyTender = "nv_isemergencytender";
        public const string DateAdvertised = "nv_dateadvertised";
        public const string Awarded = "nv_awarded";
        public const string WithinValidityPeriod = "nv_withinvalidityperiod";

        // Change request
        public const string ChangeRequestStatus = "nv_changerequeststatus";
        public const string ChangeApprovalValidUntil = "nv_changeapprovalvaliduntil";

        public const string CancellationReason = "nv_cancelationreason";
        public const string CancellationReasonOther = "nv_cancelationreasonother"; // Fixed typo (two 'l's)
        public const string DateCancelled = "nv_datecancelled"; // Try PascalCase again, or keep it lowercase if it passed
        public const string CancelledByExternalUserId = "nv_cancelledbyexternaluserid";

        // State codes (Dataverse standard)
        public const int StateActive = 0;
        public const int StateInactive = 1;
    }
}