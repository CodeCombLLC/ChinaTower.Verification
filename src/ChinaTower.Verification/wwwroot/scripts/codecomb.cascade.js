var __options = {};

function setChildren(child) {
    if (child.length > 0) {
        for (var j = 0; j < child.length; j++) {
            var c = $(child[j]);
            c.html('');
            var id = c.attr('id');
            for (var k = 0; k < __options[id].length; k++) {
                if (!__options[id][k] || __options[id][k]['data-parent-id'] == $('#' + c.attr('data-parent-select')).val())
                    c.append($('<option></option>').val(__options[id][k].value).text(__options[id][k].text));
            }
            setChildren($('select[data-parent-select="' + c.attr('id') + '"]'));
        }
    }
    else return;
}

$(document).ready(function () {
    var selects = $('select[data-parent-select]');
    for (var i = 0; i < selects.length; i++) {
        var dom = $(selects[i]);
        var id = dom.attr('data-parent-select');
        var srcdom = $('#' + id);
        if (__options[dom.attr('id')]) continue;
        __options[dom.attr('id')] = [];
        var options = dom.find('option');
        for (var j = 0; j < options.length; j++) {
            var opt = $(options[j]);
            __options[dom.attr('id')].push({
                'data-parent-id': opt.attr('data-parent-id'),
                'value': opt.attr('value'),
                'text': opt.text()
            });
        }
        srcdom.change(function () {
            $('select[data-parent-select="' + $(this).attr('id') + '"]').html('');
            var _id = $('select[data-parent-select="' + $(this).attr('id') + '"]').attr('id');
            var src = $(this);
            for (var j = 0; j < __options[_id].length; j++) {
                if (!__options[_id][j] || __options[_id][j]['data-parent-id'] == src.val()) {
                    $('select[data-parent-select="' + $(this).attr('id') + '"]').append('<option value="' + __options[_id][j].value + '">' + __options[_id][j].text + '</option>');
                }
            }
            var child = $('select[data-parent-select="' + $('select[data-parent-select="' + $(this).attr('id') + '"]').attr('id') + '"]');
            setChildren(child);
        });
    }
    for (var i = 0; i < selects.length; i++) {
        var dom = $(selects[i]);
        var id = dom.attr('data-parent-select');
        var srcdom = $('#' + id);
        srcdom.change();
    }
});