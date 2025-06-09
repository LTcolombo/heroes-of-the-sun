use std::str::FromStr;

use bolt_lang::*;

declare_id!("8va4yKEBACkT49C9wo94gS8ZaTdUrq2ipLgZvSNxWbd3");

#[component]
pub struct SmartObjectTokenLauncher {
    pub system: Pubkey,
    pub mint: Pubkey,
}

impl Default for SmartObjectTokenLauncher {
    fn default() -> Self {
        let system_program_id =
            Pubkey::from_str("DUW1KczxcpeTEY7j9nkvcuAdWGNWoadTeDBKN5Z9xhst").unwrap();

        Self::new(SmartObjectTokenLauncherInit {
            system: system_program_id,
            mint: Pubkey::default(),
        })
    }
}
