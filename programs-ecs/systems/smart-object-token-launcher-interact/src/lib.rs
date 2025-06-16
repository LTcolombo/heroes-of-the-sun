use anchor_spl::associated_token::AssociatedToken;
use bolt_lang::*;

declare_id!("DUW1KczxcpeTEY7j9nkvcuAdWGNWoadTeDBKN5Z9xhst");

#[system]
pub mod smart_object_token_launcher_interact {
    use anchor_spl::token::{mint_to, spl_token, MintTo};
    use bolt_lang::solana_program::program_pack::Pack;
    use hero::Hero;
    use smart_object_token_launcher::SmartObjectTokenLauncher;

    pub fn execute(
        ctx: Context<Components>,
        args: SmartObjectTokenLauncherInteractionArgs,
    ) -> Result<Components> {
        let mint_account = ctx
            .mint_account()
            .map_err(|_| ProgramError::InvalidAccountData)?;
        msg!("mint_account: {}", mint_account.key);

        let mint_authority = ctx
            .mint_authority()
            .map_err(|_| ProgramError::InvalidAccountData)?;
        msg!("mint_authority: {}", mint_authority.key);

        let associated_token_account = ctx
            .associated_token_account()
            .map_err(|_| ProgramError::InvalidAccountData)?;
        msg!("associated_token_account: {}", associated_token_account.key);

        let token_program = ctx
            .token_program()
            .map_err(|_| ProgramError::InvalidAccountData)?;

        msg!("token_program: {}", token_program.key);

        let launcher = &mut ctx.accounts.launcher;
        // let hero = &mut ctx.accounts.hero;

        let mint_account_key = mint_account.key();

        let (_, bump) = Pubkey::find_program_address(
            &[
                b"authority",
                mint_account_key.as_ref(), // Same seeds as macro
            ],
            ctx.program_id, // your program's id
        );

        // PDA signer seeds
        let signer_seeds: &[&[u8]] = &[
            b"authority",
            mint_account_key.as_ref(),
            &[bump], // bump always last
        ];

        // Invoke the mint_to instruction on the token program
        mint_to(
            CpiContext::new(
                token_program.to_account_info(),
                MintTo {
                    mint: mint_account.to_account_info(),
                    to: associated_token_account.to_account_info(),
                    authority: mint_authority.to_account_info(), // PDA mint authority, required as signer
                },
            )
            .with_signer(&[signer_seeds]), // using PDA to sign
            args.quantity as u64 * 10u64.pow(9 as u32),
        )?; //* 10u64.pow(mint_account.decimals as u32), // Mint tokens, adjust for decimals

        msg!("Token minted successfully.");

        Ok(ctx.accounts)
    }

    #[system_input]
    pub struct Components {
        pub launcher: SmartObjectTokenLauncher,
        // pub hero: Hero,
    }

    #[arguments]
    struct SmartObjectTokenLauncherInteractionArgs {
        pub quantity: i16,
    }

    #[extra_accounts]
    pub struct ExtraAccounts {
        #[account(mut)]
        pub payer: Signer<'info>,

        #[account(
            init_if_needed,
            space=spl_token::state::Account::LEN,
            payer = payer,
            // associated_token::mint = mint_account,
            // associated_token::authority = payer,
        )]
        associated_token_account: Account<'info, TokenAccount>,

        // Create mint account
        #[account()]
        pub mint_account: Account<'info, Mint>,

        #[account(
            mut,
            seeds = [b"authority", mint_account.key().as_ref()],
            bump,
        )]
        pub mint_authority: UncheckedAccount<'info>,

        pub token_program: Program<'info, Token>,

        pub associated_token_program: Program<'info, AssociatedToken>,
        pub system_program: Program<'info, System>,
    }
}


