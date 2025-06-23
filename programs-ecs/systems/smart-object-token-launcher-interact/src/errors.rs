use bolt_lang::error_code;

#[error_code]
pub enum TokenLauncherInteractError {
    #[msg("Mint Address Mismatch")]
    MintAddressMismatch,

    #[msg("Not enough resources in the backpack")]
    NotEnoughBackpackResources,
}
