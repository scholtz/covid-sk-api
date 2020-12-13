using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Invoice issued by the provider company to the PlaceProvider
    /// </summary>
    public class Invoice
    {
        /// <summary>
        /// ID eg 2010100001 .. the invoice number must be increment by one
        /// </summary>
        public int InvoiceId { get; set; }
        /// <summary>
        /// Date of issueance
        /// </summary>
        public DateTimeOffset IssuedOn { get; set; }
        /// <summary>
        /// Date when invoice must be payable
        /// </summary>
        public DateTimeOffset Payable { get; set; }
        /// <summary>
        /// Currency
        /// </summary>
        public string Currency { get; set; }
        /// <summary>
        /// Total price without VAT
        /// </summary>
        public decimal PriceNoVATTotal { get; set; }
        /// <summary>
        /// Total price with VAT
        /// </summary>
        public decimal PriceWithVATTotal { get; set; }
        /// <summary>
        /// Buyer VAT
        /// </summary>
        public string BuyerVAT { get; set; }
        /// <summary>
        /// Buyer name
        /// </summary>
        public string BuyerName { get; set; }
        /// <summary>
        /// Buyer trade registry ID
        /// </summary>
        public string BuyerID { get; set; }
        /// <summary>
        /// PlaceProvider identifier
        /// </summary>
        public string PlaceProviderId { get; set; }
        /// <summary>
        /// Invoice description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// SLA id
        /// </summary>
        public string SLAId { get; set; }
        /// <summary>
        /// SLA no vat price
        /// </summary>
        public decimal PriceNoVATSLA { get; set; }
        /// <summary>
        /// SLA vat price
        /// </summary>
        public decimal PriceWithVATSLA { get; set; }
        /// <summary>
        /// Number of registrations purchased
        /// </summary>
        public int Registrations { get; set; }
        /// <summary>
        /// Registrations no vat price
        /// </summary>
        public decimal PriceNoVATRegistrations { get; set; }
        /// <summary>
        /// Registrations w vat price
        /// </summary>
        public decimal PriceWithVATRegistrations { get; set; }
    }
    /// <summary>
    /// Prepayment invoice
    /// </summary>
    public class ProformaInvoice : Invoice
    {

    }
}
