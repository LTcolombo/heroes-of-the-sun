use bolt_lang::*;

declare_id!("DUW1KczxcpeTEY7j9nkvcuAdWGNWoadTeDBKN5Z9xhst");

#[system]
pub mod smart_object_token_launcher_interact {
    use smart_object_token_launcher::SmartObjectTokenLauncher;

    pub fn execute(ctx: Context<Components>, _args_p: Vec<u8>) -> Result<Components> {
        let launcher = &mut ctx.accounts.launcher;

        Ok(ctx.accounts)
    }

    #[system_input]
    pub struct Components {
        pub launcher: SmartObjectTokenLauncher,
    }
}
