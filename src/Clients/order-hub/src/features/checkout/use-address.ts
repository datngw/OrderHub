"use client";

import { useState, useEffect, useMemo, useCallback } from "react";
import type { AddressData, Province, District, Ward } from "./address-types";

let cachedData: AddressData | null = null;

async function loadAddressData(): Promise<AddressData> {
  if (cachedData) return cachedData;
  const response = await fetch("/data/addresses.json");
  if (!response.ok) throw new Error("Failed to load address data");
  const data: AddressData = await response.json();
  cachedData = data;
  return data;
}

export function useAddress(
  selectedProvinceId: number | null,
  selectedDistrictId: number | null
) {
  const [addressData, setAddressData] = useState<AddressData | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadAddressData()
      .then(setAddressData)
      .catch(() => {
        // silently fail
      })
      .finally(() => setIsLoading(false));
  }, []);

  const provinces = useMemo<Province[]>(
    () => addressData?.provinces ?? [],
    [addressData]
  );

  const districts = useMemo<District[]>(() => {
    if (!addressData || !selectedProvinceId) return [];
    return addressData.districts.filter(
      (d) => d.province_id === selectedProvinceId
    );
  }, [addressData, selectedProvinceId]);

  const wards = useMemo<Ward[]>(() => {
    if (!addressData || !selectedDistrictId) return [];
    return addressData.wards.filter((w) => w.district_id === selectedDistrictId);
  }, [addressData, selectedDistrictId]);

  const getProvinceName = useCallback(
    (id: number | null): string => {
      if (!id || !addressData) return "";
      return addressData.provinces.find((p) => p.id === id)?.name ?? "";
    },
    [addressData]
  );

  const getDistrictName = useCallback(
    (id: number | null): string => {
      if (!id || !addressData) return "";
      return addressData.districts.find((d) => d.id === id)?.name ?? "";
    },
    [addressData]
  );

  const getWardName = useCallback(
    (id: number | null): string => {
      if (!id || !addressData) return "";
      return addressData.wards.find((w) => w.id === id)?.name ?? "";
    },
    [addressData]
  );

  return {
    provinces,
    districts,
    wards,
    isLoading,
    getProvinceName,
    getDistrictName,
    getWardName,
  };
}
