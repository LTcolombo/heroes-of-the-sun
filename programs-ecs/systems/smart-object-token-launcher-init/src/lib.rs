use bolt_lang::*;
mod errors;

declare_id!("AdrPpoYr67ZcDZsQxsPgeosE3sQbZxercbUn8i1dcvap");

#[system]
pub mod smart_object_token_launcher_init {

    use smart_object_token_launcher::SmartObjectTokenLauncher;

    pub fn execute(
        ctx: Context<Components>,
        args: SmartObjectTokenLauncherInitArgs,
    ) -> Result<Components> {
        msg!("execute {}", Pubkey::new_from_array(args.mint));
        let launcher = &mut ctx.accounts.smart_object_token_launcher;

        if launcher.mint != Pubkey::default() {
            return err!(errors::SmartObjectTokenLauncherInitError::AlreadyInitialized);
        }

        msg!("mint {}", Pubkey::new_from_array(args.mint));
        launcher.mint = Pubkey::new_from_array(args.mint);
        Ok(ctx.accounts)
    }

    #[system_input]
    pub struct Components {
        pub smart_object_token_launcher: SmartObjectTokenLauncher,
    }

    #[arguments]
    struct SmartObjectTokenLauncherInitArgs {
        pub mint: [u8; 32],
    }
}
