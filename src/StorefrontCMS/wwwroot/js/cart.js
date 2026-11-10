/* Serialize mutations in this tab; server session is intentionally single-instance. */
$(function () {
 let busy = false;
 function show(data) {
  $('#cart-count').text(data.count); $('#cart-total').text(data.total.toFixed(2));
  $('.cart-row').each(function () {
   const row = $(this), item = data.items.find(x => x.id === Number(row.data('id')));
   if (!item) row.remove(); else {row.find('.line-total').text(item.total.toFixed(2)); row.find('.cart-quantity').val(item.quantity);}
  });
 }
 $.getJSON('/Cart/Summary').done(show).fail(function () { $('#cart-message').text('Could not load the cart. Refresh to retry.'); });
 function change(id, quantity, add) {
  if (busy) return;
  if (!Number.isInteger(quantity) || quantity < 0 || quantity > 99) {$('#cart-message').text('Enter a whole quantity from 1 to 99, or remove the item.');return;}
  busy = true; const buttons = $('.cart-add,.cart-update,.cart-remove').filter(':enabled'); buttons.prop('disabled', true);
  $.ajax({url:'/Cart/Set',method:'POST',data:{productId:id,quantity:quantity,add:add},headers:{'X-CSRF-TOKEN':$('#csrf input').val()}})
   .done(function (data) {show(data);$('#cart-message').attr('class','alert alert-success').text('Cart updated.');})
   .fail(function (xhr) {$('#cart-message').attr('class','alert alert-danger').text(xhr.responseJSON?.error || 'Could not update cart. Refresh and retry.');})
   .always(function () {busy=false;buttons.prop('disabled',false);});
 }
 $('.cart-add').on('click',function () {change(Number($(this).data('id')),1,true);});
 $('.cart-update').on('click',function () {const row=$(this).closest('.cart-row');change(Number(row.data('id')),Number(row.find('input').val()),false);});
 $('.cart-remove').on('click',function () {change(Number($(this).closest('.cart-row').data('id')),0,false);});
});
