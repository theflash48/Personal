package com.theflash48.ApiSpringBoot.model;

import jakarta.persistence.*;
import java.time.LocalDateTime;

@Entity
@Table(name = "accounts")
public class Account {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @Column(name = "acc_id")
    private Long accId;

    @Column(name = "username", nullable = false, unique = true, length = 32)
    private String username;

    @Column(name = "password_hash", nullable = false, length = 255)
    private String passwordHash;

    @Column(name = "wins", nullable = false)
    private int wins;

    @Column(name = "games_played", nullable = false)
    private int gamesPlayed;

    @Column(name = "kills", nullable = false)
    private int kills;

    @Column(name = "deaths", nullable = false)
    private int deaths;

    @Column(name = "fav_map_id")
    private Integer favMapId;

    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    public Account() {
    }

    public Account(Long accId,
                   String username,
                   String passwordHash,
                   int wins,
                   int gamesPlayed,
                   int kills,
                   int deaths,
                   Integer favMapId,
                   LocalDateTime createdAt) {
        this.accId = accId;
        this.username = username;
        this.passwordHash = passwordHash;
        this.wins = wins;
        this.gamesPlayed = gamesPlayed;
        this.kills = kills;
        this.deaths = deaths;
        this.favMapId = favMapId;
        this.createdAt = createdAt;
    }

    public Long getAccId() {
        return accId;
    }

    public void setAccId(Long accId) {
        this.accId = accId;
    }

    public String getUsername() {
        return username;
    }

    public void setUsername(String username) {
        this.username = username;
    }

    public String getPasswordHash() {
        return passwordHash;
    }

    public void setPasswordHash(String passwordHash) {
        this.passwordHash = passwordHash;
    }

    public int getWins() {
        return wins;
    }

    public void setWins(int wins) {
        this.wins = wins;
    }

    public int getGamesPlayed() {
        return gamesPlayed;
    }

    public void setGamesPlayed(int gamesPlayed) {
        this.gamesPlayed = gamesPlayed;
    }

    public int getKills() {
        return kills;
    }

    public void setKills(int kills) {
        this.kills = kills;
    }

    public int getDeaths() {
        return deaths;
    }

    public void setDeaths(int deaths) {
        this.deaths = deaths;
    }

    public Integer getFavMapId() {
        return favMapId;
    }

    public void setFavMapId(Integer favMapId) {
        this.favMapId = favMapId;
    }

    public LocalDateTime getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(LocalDateTime createdAt) {
        this.createdAt = createdAt;
    }
}
