using System.ComponentModel.DataAnnotations;
using StorefrontCMS.Models;
using Xunit;
namespace StorefrontCMS.Tests;
public sealed class CheckoutValidationTests {
 [Theory][InlineData(false,false)][InlineData(true,true)]
 public void Mock_payment_requires_confirmation(bool confirm,bool expected) {
 var input=new CheckoutInput {RequestId=Guid.NewGuid(),Confirm=confirm};
 Assert.Equal(expected,Validator.TryValidateObject(input,new ValidationContext(input),new List<ValidationResult>(),true));
 }
}
