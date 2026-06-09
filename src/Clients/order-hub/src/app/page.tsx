import { cookies } from "next/headers";
import { redirect } from "next/navigation";

export default async function RootPage() {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get("access_token")?.value;

  if (accessToken) {
    // Check role from user cookie or redirect to shop homepage
    redirect("/products");
  }

  redirect("/products");
}
