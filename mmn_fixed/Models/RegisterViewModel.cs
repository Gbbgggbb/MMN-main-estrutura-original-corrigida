using System.ComponentModel.DataAnnotations;

namespace MMN.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha.")]
    [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme sua senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "As senhas nao conferem.")]
    [Display(Name = "Confirmar senha")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
