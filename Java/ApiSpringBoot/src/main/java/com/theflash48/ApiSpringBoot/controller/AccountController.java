package com.theflash48.ApiSpringBoot.controller;

import com.theflash48.ApiSpringBoot.model.Account;
import com.theflash48.ApiSpringBoot.repository.AccountRepository;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.time.LocalDateTime;
import java.util.List;


@RestController
@RequestMapping("/accounts")
public class AccountController {

    private final AccountRepository accountRepository;

    // Inyección por constructor: Spring nos da el repositorio ya preparado
    public AccountController(AccountRepository accountRepository) {
        this.accountRepository = accountRepository;
    }

    @GetMapping("/test")
    public Account getTestAccount() {
        return new Account(
                1L,
                "flash",
                "FAKE_HASH",
                10,
                25,
                100,
                50,
                null,
                LocalDateTime.now()
        );
    }

    // GET /accounts/all → lista completa desde la BD
    @GetMapping("/all")
    public List<Account> getAllAccounts() {
        return accountRepository.findAll();
    }

    // GET /accounts/by-username/{username} devuelve el usuario que le indiquemos
    @GetMapping("/by-username/{username}")
    public Account getByUsername(@PathVariable String username) {
        return accountRepository.findByUsername(username)
                .orElse(null); // luego ya haremos manejo de errores mejor
    }

}
