function deleteDialog(url, id) {
    var html = '<div class="message-bg"></div><div class="message-outer"><div class="message-container">' +
        '<h3>提示信息</h3>' +
        '<p>点击删除按钮后，该记录将被永久删除，您确定要这样做吗？</p>' +
        '<div class="message-buttons"><a href="javascript:Delete(\'' + url + '\');deleteRow(\'' + id + '\')" class="btn btn-danger">删除</a> <a href="javascript:closeDialog()" class="btn">取消</a></div>' +
        '</div></div>';
    var dom = $(html);
    $('body').append(dom);
    $(".message-outer").css('margin-top', -($(".message-outer").outerHeight() / 2));
    setTimeout(function () { $(".message-bg").addClass('active'); $(".message-outer").addClass('active'); }, 10);
}

function closeDialog() {
    $('.message-bg').removeClass('active');
    $('.message-outer').removeClass('active');
    setTimeout(function () {
        $('.message-bg').remove();
        $('.message-outer').remove();
    }, 200);
}

function deleteRow(id)
{
    $('#' + id).remove();
    closeDialog();
}


var pos;

function insertRule(p)
{
    pos = $(p).parent();
    console.log(pos);
    $('#modalInsertRule').modal('show');
}

$(document).ready(function () {
    $('#lstRuleTypes').change(function () {
        switch ($('#lstRuleTypes').val()) {
            case 'And':
            case 'Or':
            case 'Not':
                $('#divIndexSelector').hide();
                $('#divExpression').hide();
                break;
            case 'Empty':
            case 'NotEmpty':
                $('#divIndexSelector').show();
                $('#divExpression').hide();
                break;
            default:
                $('#divIndexSelector').show();
                $('#divExpression').show();
                break;
        }
    });

    $('#btnInsertRule').click(function () {
        var str = '<li data-type="' + $('#lstRuleTypes').find('option:selected').text() + '" data-expression="' + $('#txtExpression').val() + '" data-argument-index="' + $('#lstHeaders').val() + '">';
        switch ($('#lstRuleTypes').val())
        {
            case 'And':
                str += '<span>满足下面的全部子条件</span>';
                break;
            case 'Or':
                str += '<span>满足下面的任意一个子条件</span>';
                break;
            case 'Not':
                str += '<span>不可符合下列任意一个子条件</span>';
                break;
            case 'Equal':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' = ' + $('#txtExpression').val() + '</span>';
                break;
            case 'NotEqual':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' ≠ ' + $('#txtExpression').val() + '</span>';
                break;
            case 'Gt':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' ＞ ' + $('#txtExpression').val() + '</span>';
                break;
            case 'Gte':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' ≥ ' + $('#txtExpression').val() + '</span>';
                break;
            case 'Lt':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' ＜ ' + $('#txtExpression').val() + '</span>';
                break;
            case 'Lte':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' ≤ ' + $('#txtExpression').val() + '</span>';
                break;
            case 'Empty':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' 必须为空</span>';
                break;
            case 'NotEmpty':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' 不能为空</span>';
                break;
            case 'Regex':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' 满足正则表达式 ' + $('#txtExpression').val() + '</span>';
                break;
        }
        str += ' <a href="javascript:$(this).parent(\'li\').remove()">删除</a>';
        if ($('#lstRuleTypes').val() == 'And' || $('#lstRuleTypes').val() == 'Or' || $('#lstRuleTypes').val() == 'Not')
            str += '<ul><li class="li-insert"><a onclick="insertRule(this)" href="javascript:;">添加</a></li></ul>';
        str += '</li>';
        pos.before(str);
        $('#modalInsertRule').modal('hide');
    });
});