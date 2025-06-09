use bolt_lang::error_code;

#[error_code]
pub enum SmartObjectTokenLauncherInitError {
    #[msg("Already Initialized")]
    AlreadyInitialized,
}
