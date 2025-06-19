use bolt_lang::error_code;

#[error_code]
pub enum SmartObjectInitError {
    #[msg("Already Initialized")]
    AlreadyInitialized,
}
